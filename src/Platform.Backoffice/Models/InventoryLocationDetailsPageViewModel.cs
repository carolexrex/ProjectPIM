using Platform.Contracts.Catalog.Inventory;

namespace Platform.Backoffice.Models;

public sealed class InventoryLocationDetailsPageViewModel
{
    public InventoryLocationUpdateViewModel Location { get; init; } = new();
    public IReadOnlyList<InventoryLocationMarketAssignmentDto> Markets { get; init; } = [];
    public IReadOnlyList<InventoryBalanceDto> Balances { get; init; } = [];
    public IReadOnlyList<InventoryTransactionDto> RecentTransactions { get; init; } = [];
    public InventoryLocationMarketAssignmentCreateViewModel MarketAssignmentForm { get; init; } = new();
    public InventoryBalanceUpsertViewModel BalanceForm { get; init; } = new();
    public InventoryAdjustmentCreateViewModel AdjustmentForm { get; init; } = new();
}
