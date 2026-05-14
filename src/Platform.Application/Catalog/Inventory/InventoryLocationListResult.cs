using Platform.Domain.Catalog.Inventory;

namespace Platform.Application.Catalog.Inventory;

public sealed record InventoryLocationListResult(
    IReadOnlyList<InventoryLocation> Items,
    int Total);
