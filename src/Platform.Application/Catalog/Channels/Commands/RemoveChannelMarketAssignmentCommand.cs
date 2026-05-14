namespace Platform.Application.Catalog.Channels.Commands;

public sealed record RemoveChannelMarketAssignmentCommand(
    Guid ChannelId,
    Guid MarketId,
    string RowVersion);
