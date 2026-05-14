namespace Platform.Domain.Catalog.Channels;

public sealed class ChannelMarketAssignment
{
    private ChannelMarketAssignment()
    {
        Id = Guid.Empty;
        MarketId = Guid.Empty;
    }

    public ChannelMarketAssignment(Guid id, Guid marketId)
    {
        Id = id;
        MarketId = marketId;
    }

    public Guid Id { get; private set; }
    public Guid MarketId { get; private set; }
}
