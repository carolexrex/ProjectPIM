using Platform.Application.Catalog.Inventory;
using Platform.Domain.Catalog.Inventory;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class InMemoryInventoryBalanceRepository : IInventoryBalanceRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryInventoryBalanceRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<InventoryBalance?> GetByInventoryLocationAndVariantAsync(Guid inventoryLocationId, Guid variantId, CancellationToken cancellationToken)
    {
        var balance = _store.InventoryBalances.Values.FirstOrDefault(x => x.InventoryLocationId == inventoryLocationId && x.VariantId == variantId);
        return Task.FromResult(balance);
    }

    public Task<IReadOnlyList<InventoryBalance>> ListByInventoryLocationAsync(Guid inventoryLocationId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InventoryBalance>>(
            _store.InventoryBalances.Values.Where(x => x.InventoryLocationId == inventoryLocationId).OrderBy(x => x.VariantId).ToList());
    }

    public Task<IReadOnlyList<InventoryBalance>> ListByVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InventoryBalance>>(
            _store.InventoryBalances.Values.Where(x => x.VariantId == variantId).OrderBy(x => x.InventoryLocationId).ToList());
    }

    public Task<IReadOnlyList<InventoryTransaction>> ListTransactionsByInventoryLocationAsync(Guid inventoryLocationId, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InventoryTransaction>>(
            _store.InventoryTransactions.Values
                .Where(x => x.InventoryLocationId == inventoryLocationId)
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(take)
                .ToList());
    }

    public Task<IReadOnlyDictionary<Guid, int>> CountByInventoryLocationIdsAsync(IReadOnlyCollection<Guid> inventoryLocationIds, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, int> counts = _store.InventoryBalances.Values
            .Where(x => inventoryLocationIds.Contains(x.InventoryLocationId))
            .GroupBy(x => x.InventoryLocationId)
            .ToDictionary(x => x.Key, x => x.Count());

        return Task.FromResult(counts);
    }

    public Task AddAsync(InventoryBalance inventoryBalance, CancellationToken cancellationToken)
    {
        _store.InventoryBalances[inventoryBalance.Id] = inventoryBalance;
        return Task.CompletedTask;
    }

    public Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken)
    {
        _store.InventoryTransactions[transaction.Id] = transaction;
        return Task.CompletedTask;
    }
}
