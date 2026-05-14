using Platform.Domain.Catalog.Inventory;

namespace Platform.Application.Catalog.Inventory;

public interface IInventoryBalanceRepository
{
    Task<InventoryBalance?> GetByInventoryLocationAndVariantAsync(Guid inventoryLocationId, Guid variantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryBalance>> ListByInventoryLocationAsync(Guid inventoryLocationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryBalance>> ListByVariantAsync(Guid variantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryTransaction>> ListTransactionsByInventoryLocationAsync(Guid inventoryLocationId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, int>> CountByInventoryLocationIdsAsync(IReadOnlyCollection<Guid> inventoryLocationIds, CancellationToken cancellationToken);
    Task AddAsync(InventoryBalance inventoryBalance, CancellationToken cancellationToken);
    Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken cancellationToken);
}
