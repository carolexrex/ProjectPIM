using Platform.Domain.Common;

namespace Platform.Domain.Cart;

public sealed class Cart
{
    private readonly List<CartLine> _lines = [];
    private readonly List<CartAddress> _addresses = [];

    private Cart()
    {
        Id = Guid.Empty;
        CurrencyCode = string.Empty;
        CultureCode = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public Cart(
        Guid id,
        Guid? customerId,
        Guid? companyId,
        Guid marketId,
        string currencyCode,
        string cultureCode,
        string? email,
        DateTime? expiresAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        CompanyId = companyId;
        MarketId = marketId;
        CurrencyCode = NormalizeRequired(currencyCode).ToUpperInvariant();
        CultureCode = NormalizeRequired(cultureCode);
        Email = NormalizeOptional(email);
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Status = "Active";
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid MarketId { get; private set; }
    public string CurrencyCode { get; private set; }
    public string CultureCode { get; private set; }
    public string? Email { get; private set; }
    public string Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal VatTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<CartLine> Lines => _lines;
    public IReadOnlyCollection<CartAddress> Addresses => _addresses;

    public CartLine AddLine(
        Guid variantId,
        string sku,
        string productName,
        string? variantDescription,
        decimal quantity,
        decimal unitPrice,
        decimal vatRate,
        string? comment)
    {
        EnsureEditable();

        var line = new CartLine(Guid.NewGuid(), Id, variantId, sku, productName, variantDescription, quantity, unitPrice, vatRate, comment);
        _lines.Add(line);
        RecalculateTotals();
        Touch();
        return line;
    }

    public CartAddress AddAddress(
        string type,
        string firstName,
        string lastName,
        string? companyName,
        string line1,
        string? line2,
        string postalCode,
        string city,
        string? region,
        string countryCode,
        string? email,
        string? phone)
    {
        EnsureEditable();

        var existing = _addresses.FirstOrDefault(x => string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _addresses.Remove(existing);
        }

        var address = new CartAddress(Guid.NewGuid(), Id, type, firstName, lastName, companyName, line1, line2, postalCode, city, region, countryCode, email, phone);
        _addresses.Add(address);
        Touch();
        return address;
    }

    public void Reprice(IReadOnlyCollection<CartPriceUpdate> updates, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        EnsureEditable();

        foreach (var line in _lines)
        {
            var update = updates.FirstOrDefault(x => x.CartLineId == line.Id);
            if (update is null)
            {
                continue;
            }

            line.Reprice(update.Sku, update.ProductName, update.VariantDescription, update.UnitPrice, update.VatRate, update.Comment);
        }

        RecalculateTotals();
        Touch();
    }

    public void Expire(string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Status = "Expired";
        ExpiresAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Convert(string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        EnsureEditable();
        Status = "Converted";
        ExpiresAtUtc = DateTime.UtcNow;
        Touch();
    }

    private void EnsureEditable()
    {
        if (!string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The cart is no longer editable.");
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
            throw new ConcurrencyException("The cart has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string NewRowVersion()
    {
        return System.Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
