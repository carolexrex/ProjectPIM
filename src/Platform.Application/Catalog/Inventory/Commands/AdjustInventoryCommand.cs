namespace Platform.Application.Catalog.Inventory.Commands;

public sealed record AdjustInventoryCommand(
    Guid InventoryLocationId,
    Guid VariantId,
    string Type,
    decimal QuantityDelta,
    string ReferenceType,
    Guid? ReferenceId);
