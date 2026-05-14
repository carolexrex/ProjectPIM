namespace Platform.Application.Catalog.Inventory.Commands;

public sealed record CreateInventoryLocationCommand(
    string Code,
    string Name,
    string Type,
    string? CountryCode);
