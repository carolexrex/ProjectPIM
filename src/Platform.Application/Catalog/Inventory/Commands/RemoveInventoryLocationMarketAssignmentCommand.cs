namespace Platform.Application.Catalog.Inventory.Commands;

public sealed record RemoveInventoryLocationMarketAssignmentCommand(
    Guid InventoryLocationId,
    Guid MarketId,
    string RowVersion);
