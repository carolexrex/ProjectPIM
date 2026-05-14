namespace Platform.Application.Catalog.Inventory.Commands;

public sealed record UpsertInventoryBalanceCommand(
    Guid InventoryLocationId,
    Guid VariantId,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal IncomingQuantity,
    bool Backorderable,
    string? RowVersion);
