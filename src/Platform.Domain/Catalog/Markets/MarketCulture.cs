namespace Platform.Domain.Catalog.Markets;

public sealed class MarketCulture
{
    private MarketCulture()
    {
        Id = Guid.Empty;
        CultureCode = string.Empty;
    }

    public MarketCulture(Guid id, string cultureCode, bool isDefault)
    {
        Id = id;
        CultureCode = cultureCode;
        IsDefault = isDefault;
    }

    public Guid Id { get; private set; }
    public string CultureCode { get; private set; }
    public bool IsDefault { get; private set; }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }
}
