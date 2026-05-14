namespace Platform.Application.Catalog.Inventory.Commands;

public sealed record UpsertInventoryLocationMarketAssignmentCommand(
    Guid InventoryLocationId,
    Guid MarketId,
    int Priority,
    string RowVersion);
