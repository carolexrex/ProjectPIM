namespace Platform.Domain.Orders;

public sealed class PaymentTransaction
{
    private PaymentTransaction()
    {
        Id = Guid.Empty;
        OrderId = Guid.Empty;
        Provider = string.Empty;
        ProviderReference = string.Empty;
        Type = string.Empty;
        Status = string.Empty;
        CurrencyCode = string.Empty;
    }

    public PaymentTransaction(
        Guid id,
        Guid orderId,
        string provider,
        string providerReference,
        string type,
        string status,
        decimal amount,
        string currencyCode,
        DateTime requestedAtUtc,
        DateTime? completedAtUtc)
    {
        Id = id;
        OrderId = orderId;
        Provider = NormalizeRequired(provider);
        ProviderReference = NormalizeRequired(providerReference);
        Type = NormalizeRequired(type);
        Status = NormalizeRequired(status);
        Amount = amount;
        CurrencyCode = NormalizeRequired(currencyCode).ToUpperInvariant();
        RequestedAtUtc = requestedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; }
    public string ProviderReference { get; private set; }
    public string Type { get; private set; }
    public string Status { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
