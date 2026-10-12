using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Inventory;
using Platform.Domain.Catalog.Inventory;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class EfInventoryAllocationRepository : IInventoryAllocationRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfInventoryAllocationRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<InventoryAllocation>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryAllocations
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> SumOutstandingByBalanceAsync(Guid inventoryBalanceId, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryAllocations
            .AsNoTracking()
            .Where(x => x.InventoryBalanceId == inventoryBalanceId && x.Status == InventoryAllocation.ReservedStatus)
            .SumAsync(x => x.Quantity, cancellationToken);
    }

    public async Task AddAsync(InventoryAllocation allocation, CancellationToken cancellationToken)
    {
        await _dbContext.InventoryAllocations.AddAsync(allocation, cancellationToken);
    }
}
