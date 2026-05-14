using Platform.Contracts.Catalog.Inventory;

namespace Platform.Backoffice.Models;

public sealed class InventoryLocationListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<InventoryLocationSummaryDto> Locations { get; init; } = [];
    public int Total { get; init; }
}
