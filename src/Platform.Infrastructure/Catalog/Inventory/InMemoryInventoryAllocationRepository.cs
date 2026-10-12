using Platform.Application.Catalog.Inventory;
using Platform.Domain.Catalog.Inventory;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class InMemoryInventoryAllocationRepository : IInventoryAllocationRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryInventoryAllocationRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<InventoryAllocation>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InventoryAllocation>>(
            _store.InventoryAllocations.Values
                .Where(x => x.OrderId == orderId)
                .OrderBy(x => x.CreatedAtUtc)
                .ThenBy(x => x.Id)
                .ToList());
    }

    public Task<decimal> SumOutstandingByBalanceAsync(Guid inventoryBalanceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _store.InventoryAllocations.Values
                .Where(x => x.InventoryBalanceId == inventoryBalanceId && x.IsOutstanding)
                .Sum(x => x.Quantity));
    }

    public Task AddAsync(InventoryAllocation allocation, CancellationToken cancellationToken)
    {
        _store.InventoryAllocations[allocation.Id] = allocation;
        return Task.CompletedTask;
    }
}
