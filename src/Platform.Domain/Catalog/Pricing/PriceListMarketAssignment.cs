namespace Platform.Domain.Catalog.Pricing;

public sealed class PriceListMarketAssignment
{
    private PriceListMarketAssignment()
    {
        Id = Guid.Empty;
        MarketId = Guid.Empty;
    }

    public PriceListMarketAssignment(Guid id, Guid marketId, int priority, bool isBasePriceList)
    {
        Id = id;
        MarketId = marketId;
        Priority = priority;
        IsBasePriceList = isBasePriceList;
    }

    public Guid Id { get; private set; }
    public Guid MarketId { get; private set; }
    public int Priority { get; private set; }
    public bool IsBasePriceList { get; private set; }

    public void Update(int priority, bool isBasePriceList)
    {
        Priority = priority;
        IsBasePriceList = isBasePriceList;
    }
}
