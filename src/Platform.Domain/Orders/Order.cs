using Platform.Domain.Common;

namespace Platform.Domain.Orders;

public sealed class Order
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedStatusTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Placed"] = ["Processing", "Cancelled"],
            ["Processing"] = ["Completed", "Cancelled"],
            ["Completed"] = [],
            ["Cancelled"] = []
        };

    private readonly List<OrderLine> _lines = [];
    private readonly List<OrderAddress> _addresses = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];
    private readonly List<PaymentTransaction> _paymentTransactions = [];

    private Order()
    {
        Id = Guid.Empty;
        OrderNumber = string.Empty;
        Status = string.Empty;
        CurrencyCode = string.Empty;
        CultureCode = string.Empty;
        Email = string.Empty;
        PaymentStatus = string.Empty;
        FulfillmentStatus = string.Empty;
        RowVersion = string.Empty;
    }

    public Order(
        Guid id,
        Guid? sourceCartId,
        string orderNumber,
        Guid? customerId,
        Guid? companyId,
        Guid marketId,
        string currencyCode,
        string cultureCode,
        string email,
        DateTime placedAtUtc,
        IEnumerable<OrderLine> lines,
        IEnumerable<OrderAddress> addresses,
        string createdBy,
        string? initialComment)
    {
        Id = id;
        SourceCartId = sourceCartId;
        OrderNumber = NormalizeRequired(orderNumber);
        CustomerId = customerId;
        CompanyId = companyId;
        MarketId = marketId;
        CurrencyCode = NormalizeRequired(currencyCode).ToUpperInvariant();
        CultureCode = NormalizeRequired(cultureCode);
        Email = NormalizeRequired(email);
        PlacedAtUtc = placedAtUtc;
        CreatedAtUtc = placedAtUtc;
        UpdatedAtUtc = placedAtUtc;
        Status = "Placed";
        PaymentStatus = "Pending";
        FulfillmentStatus = "Pending";
        RowVersion = NewRowVersion();
        _lines.AddRange(lines);
        _addresses.AddRange(addresses);
        RecalculateTotals();
        _statusHistory.Add(new OrderStatusHistory(Guid.NewGuid(), Id, null, Status, createdBy, placedAtUtc, initialComment));
    }

    public Guid Id { get; private set; }
    public Guid? SourceCartId { get; private set; }
    public string OrderNumber { get; private set; }
    public string Status { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid MarketId { get; private set; }
    public string CurrencyCode { get; private set; }
    public string CultureCode { get; private set; }
    public string Email { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal VatTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public DateTime PlacedAtUtc { get; private set; }
    public string PaymentStatus { get; private set; }
    public string FulfillmentStatus { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines;
    public IReadOnlyCollection<OrderAddress> Addresses => _addresses;
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory;
    public IReadOnlyCollection<PaymentTransaction> PaymentTransactions => _paymentTransactions;

    public OrderStatusHistory ChangeStatus(string toStatus, string changedBy, string? comment, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var normalizedToStatus = NormalizeRequired(toStatus);
        if (string.Equals(Status, normalizedToStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Order status is already set to the requested value.");
        }

        if (!AllowedStatusTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(normalizedToStatus, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Illegal order state transition from {Status} to {normalizedToStatus}.");
        }

        var history = new OrderStatusHistory(Guid.NewGuid(), Id, Status, normalizedToStatus, changedBy, DateTime.UtcNow, comment);
        Status = normalizedToStatus;
        if (string.Equals(normalizedToStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            FulfillmentStatus = "Completed";
        }

        if (string.Equals(normalizedToStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            FulfillmentStatus = "Cancelled";
        }

        _statusHistory.Add(history);
        Touch();
        return history;
    }

    public PaymentTransaction AddPaymentTransaction(
        string provider,
        string providerReference,
        string type,
        string status,
        decimal amount,
        string currencyCode,
        DateTime requestedAtUtc,
        DateTime? completedAtUtc,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        var existing = _paymentTransactions.FirstOrDefault(x =>
            string.Equals(x.Provider, provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ProviderReference, providerReference, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        var transaction = new PaymentTransaction(Guid.NewGuid(), Id, provider, providerReference, type, status, amount, currencyCode, requestedAtUtc, completedAtUtc);
        _paymentTransactions.Add(transaction);
        ApplyPaymentStatus(status);
        Touch();
        return transaction;
    }

    private void ApplyPaymentStatus(string paymentTransactionStatus)
    {
        var normalized = NormalizeRequired(paymentTransactionStatus);

        if (string.Equals(normalized, "Authorized", StringComparison.OrdinalIgnoreCase))
        {
            PaymentStatus = "Authorized";
        }
        else if (string.Equals(normalized, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            PaymentStatus = "Paid";
        }
        else if (string.Equals(normalized, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            PaymentStatus = "Failed";
        }
        else if (string.Equals(normalized, "Refunded", StringComparison.OrdinalIgnoreCase))
        {
            PaymentStatus = "Refunded";
        }
    }

    private void RecalculateTotals()
    {
        Subtotal = decimal.Round(_lines.Sum(x => x.LineTotal), 2, MidpointRounding.AwayFromZero);
        VatTotal = decimal.Round(_lines.Sum(x => x.LineTotal * x.VatRate), 2, MidpointRounding.AwayFromZero);
        GrandTotal = decimal.Round(Subtotal + VatTotal, 2, MidpointRounding.AwayFromZero);
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The order has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
