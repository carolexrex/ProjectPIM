namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class InMemoryInventoryWriteCoordinator : IInventoryWriteCoordinator
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryInventoryWriteCoordinator(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await _store.InventoryWriteLock.WaitAsync(cancellationToken);
        try
        {
            return await operation(cancellationToken);
        }
        finally
        {
            _store.InventoryWriteLock.Release();
        }
    }
}
