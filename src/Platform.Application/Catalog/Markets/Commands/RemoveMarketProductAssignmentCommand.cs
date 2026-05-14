namespace Platform.Application.Catalog.Markets.Commands;

public sealed record RemoveMarketProductAssignmentCommand(
    Guid MarketId,
    Guid ProductId,
    string RowVersion);
