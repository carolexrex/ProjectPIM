namespace Platform.Application.Catalog.Markets.Commands;

public sealed record UpsertMarketProductAssignmentCommand(
    Guid MarketId,
    Guid ProductId,
    string Status,
    string RowVersion);
