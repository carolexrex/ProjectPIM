namespace Platform.Application.Catalog.Channels.Commands;

public sealed record UpsertChannelMarketAssignmentCommand(
    Guid ChannelId,
    Guid MarketId,
    string RowVersion);
