using Platform.Application.Catalog.Products;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class InMemoryProductStatusDefinitionRepository : IProductStatusDefinitionRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryProductStatusDefinitionRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<ProductStatusDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Statuses.TryGetValue(id, out var status) ? status : null);
    }
}
