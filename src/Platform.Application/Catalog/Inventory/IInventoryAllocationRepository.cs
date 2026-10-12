using Platform.Domain.Catalog.Inventory;

namespace Platform.Application.Catalog.Inventory;

public interface IInventoryAllocationRepository
{
    /// <summary>Allocations for an order, tracked for update.</summary>
    Task<IReadOnlyList<InventoryAllocation>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Sum of outstanding (reserved, unsettled) allocation quantities on a balance.</summary>
    Task<decimal> SumOutstandingByBalanceAsync(Guid inventoryBalanceId, CancellationToken cancellationToken);

    Task AddAsync(InventoryAllocation allocation, CancellationToken cancellationToken);
}
