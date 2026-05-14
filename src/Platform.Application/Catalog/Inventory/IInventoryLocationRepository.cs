using Platform.Application.Catalog.Inventory.Queries;
using Platform.Domain.Catalog.Inventory;

namespace Platform.Application.Catalog.Inventory;

public interface IInventoryLocationRepository
{
    Task<InventoryLocationListResult> ListAsync(ListInventoryLocationsQuery query, CancellationToken cancellationToken);
    Task<InventoryLocation?> GetByIdAsync(Guid inventoryLocationId, CancellationToken cancellationToken);
    Task<InventoryLocation?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryLocation>> GetByIdsAsync(IReadOnlyCollection<Guid> inventoryLocationIds, CancellationToken cancellationToken);
    Task AddAsync(InventoryLocation inventoryLocation, CancellationToken cancellationToken);
}
