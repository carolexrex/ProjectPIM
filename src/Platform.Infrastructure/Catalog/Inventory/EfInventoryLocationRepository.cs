using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Inventory.Queries;
using Platform.Domain.Catalog.Inventory;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class EfInventoryLocationRepository : IInventoryLocationRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfInventoryLocationRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryLocationListResult> ListAsync(ListInventoryLocationsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.InventoryLocations
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search) || x.Code.Contains(query.Search) || x.Name.Contains(query.Search))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => !query.MarketId.HasValue || x.MarketAssignments.Any(y => y.MarketId == query.MarketId.Value));

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.MarketAssignments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new InventoryLocationListResult(items, total);
    }

    public async Task<InventoryLocation?> GetByIdAsync(Guid inventoryLocationId, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryLocations
            .Include(x => x.MarketAssignments)
            .FirstOrDefaultAsync(x => x.Id == inventoryLocationId, cancellationToken);
    }

    public async Task<InventoryLocation?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryLocations
            .Include(x => x.MarketAssignments)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryLocation>> GetByIdsAsync(IReadOnlyCollection<Guid> inventoryLocationIds, CancellationToken cancellationToken)
    {
        if (inventoryLocationIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.InventoryLocations
            .AsNoTracking()
            .Include(x => x.MarketAssignments)
            .Where(x => inventoryLocationIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InventoryLocation inventoryLocation, CancellationToken cancellationToken)
    {
        await _dbContext.InventoryLocations.AddAsync(inventoryLocation, cancellationToken);
    }

    private static IQueryable<InventoryLocation> ApplySorting(IQueryable<InventoryLocation> locations, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => locations.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => locations.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-name" => locations.OrderByDescending(x => x.Name).ThenBy(x => x.Code),
            "name" => locations.OrderBy(x => x.Name).ThenBy(x => x.Code),
            "-type" => locations.OrderByDescending(x => x.Type).ThenBy(x => x.Code),
            "type" => locations.OrderBy(x => x.Type).ThenBy(x => x.Code),
            "-code" => locations.OrderByDescending(x => x.Code),
            _ => locations.OrderBy(x => x.Code)
        };
    }
}
