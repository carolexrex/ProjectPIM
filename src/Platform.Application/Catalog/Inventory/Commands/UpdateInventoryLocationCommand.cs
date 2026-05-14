namespace Platform.Application.Catalog.Inventory.Commands;

public sealed record UpdateInventoryLocationCommand(
    Guid InventoryLocationId,
    string Code,
    string Name,
    string Type,
    string? CountryCode,
    string RowVersion);
