namespace Platform.Domain.Catalog.Inventory;

public sealed class InventoryLocationMarketAssignment
{
    private InventoryLocationMarketAssignment()
    {
        Id = Guid.Empty;
    }

    public InventoryLocationMarketAssignment(Guid id, Guid marketId, int priority)
    {
        Id = id;
        MarketId = marketId;
        Priority = priority;
    }

    public Guid Id { get; private set; }
    public Guid MarketId { get; private set; }
    public int Priority { get; private set; }

    public void Update(int priority)
    {
        Priority = priority;
    }
}
