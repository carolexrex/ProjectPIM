using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Inventory;
using Platform.Domain.Catalog.Inventory;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class EfInventoryBalanceRepository : IInventoryBalanceRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfInventoryBalanceRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryBalance?> GetByInventoryLocationAndVariantAsync(Guid inventoryLocationId, Guid variantId, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryBalances
            .FirstOrDefaultAsync(x => x.InventoryLocationId == inventoryLocationId && x.VariantId == variantId, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryBalance>> ListByInventoryLocationAsync(Guid inventoryLocationId, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryBalances
            .AsNoTracking()
            .Where(x => x.InventoryLocationId == inventoryLocationId)
            .OrderBy(x => x.VariantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryBalance>> ListByVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryBalances
            .AsNoTracking()
            .Where(x => x.VariantId == variantId)
            .OrderBy(x => x.InventoryLocationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryBalance>> ListForUpdateAsync(
        IReadOnlyCollection<Guid> variantIds,
        IReadOnlyCollection<Guid> inventoryLocationIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0 || inventoryLocationIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.InventoryBalances
            .Where(x => variantIds.Contains(x.VariantId) && inventoryLocationIds.Contains(x.InventoryLocationId))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryBalance>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> inventoryBalanceIds, CancellationToken cancellationToken)
    {
        if (inventoryBalanceIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.InventoryBalances
            .Where(x => inventoryBalanceIds.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryTransaction>> ListTransactionsByInventoryLocationAsync(Guid inventoryLocationId, int take, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryTransactions
            .AsNoTracking()
            .Where(x => x.InventoryLocationId == inventoryLocationId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByInventoryLocationIdsAsync(IReadOnlyCollection<Guid> inventoryLocationIds, CancellationToken cancellationToken)
    {
        if (inventoryLocationIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _dbContext.InventoryBalances
            .AsNoTracking()
            .Where(x => inventoryLocationIds.Contains(x.InventoryLocationId))
            .GroupBy(x => x.InventoryLocationId)
            .ToDictionaryAsync(x => x.Key, x => x.Count(), cancellationToken);
    }

    public async Task AddAsync(InventoryBalance inventoryBalance, CancellationToken cancellationToken)
    {
        await _dbContext.InventoryBalances.AddAsync(inventoryBalance, cancellationToken);
    }

    public async Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken)
    {
        await _dbContext.InventoryTransactions.AddAsync(transaction, cancellationToken);
    }
}
