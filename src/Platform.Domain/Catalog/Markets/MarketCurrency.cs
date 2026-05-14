namespace Platform.Domain.Catalog.Markets;

public sealed class MarketCurrency
{
    private MarketCurrency()
    {
        Id = Guid.Empty;
        CurrencyCode = string.Empty;
    }

    public MarketCurrency(Guid id, string currencyCode, bool isDefault)
    {
        Id = id;
        CurrencyCode = currencyCode;
        IsDefault = isDefault;
    }

    public Guid Id { get; private set; }
    public string CurrencyCode { get; private set; }
    public bool IsDefault { get; private set; }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }
}
