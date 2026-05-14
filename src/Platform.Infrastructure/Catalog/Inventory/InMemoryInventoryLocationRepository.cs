using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Inventory.Queries;
using Platform.Domain.Catalog.Inventory;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class InMemoryInventoryLocationRepository : IInventoryLocationRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryInventoryLocationRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<InventoryLocationListResult> ListAsync(ListInventoryLocationsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _store.InventoryLocations.Values
            .Where(x => string.IsNullOrWhiteSpace(query.Search) || x.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(x => !query.MarketId.HasValue || x.MarketAssignments.Any(y => y.MarketId == query.MarketId.Value));

        filtered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => filtered.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => filtered.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-name" => filtered.OrderByDescending(x => x.Name).ThenBy(x => x.Code),
            "name" => filtered.OrderBy(x => x.Name).ThenBy(x => x.Code),
            "-type" => filtered.OrderByDescending(x => x.Type).ThenBy(x => x.Code),
            "type" => filtered.OrderBy(x => x.Type).ThenBy(x => x.Code),
            "-code" => filtered.OrderByDescending(x => x.Code),
            _ => filtered.OrderBy(x => x.Code)
        };

        var materialized = filtered.ToList();
        return Task.FromResult(new InventoryLocationListResult(materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(), materialized.Count));
    }

    public Task<InventoryLocation?> GetByIdAsync(Guid inventoryLocationId, CancellationToken cancellationToken)
    {
        _store.InventoryLocations.TryGetValue(inventoryLocationId, out var location);
        return Task.FromResult(location);
    }

    public Task<InventoryLocation?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var location = _store.InventoryLocations.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(location);
    }

    public Task<IReadOnlyList<InventoryLocation>> GetByIdsAsync(IReadOnlyCollection<Guid> inventoryLocationIds, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InventoryLocation>>(
            _store.InventoryLocations.Values.Where(x => inventoryLocationIds.Contains(x.Id)).ToList());
    }

    public Task AddAsync(InventoryLocation inventoryLocation, CancellationToken cancellationToken)
    {
        _store.InventoryLocations[inventoryLocation.Id] = inventoryLocation;
        return Task.CompletedTask;
    }
}
