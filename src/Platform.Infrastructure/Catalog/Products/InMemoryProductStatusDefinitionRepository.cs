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

    public Task<IReadOnlyList<ProductStatusDefinition>> ListAsync(
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ProductStatusDefinition> statuses = _store.Statuses.Values
            .Where(x => x.EntityType == entityType)
            .OrderBy(x => x.Name)
            .ToList();

        return Task.FromResult(statuses);
    }

    public Task<ProductStatusDefinition?> GetByIdAsync(
        Guid id,
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _store.Statuses.TryGetValue(id, out var status) && status.EntityType == entityType
                ? status
                : null);
    }

    public Task<ProductStatusDefinition?> GetByCodeAsync(
        string code,
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _store.Statuses.Values.FirstOrDefault(
                x => x.EntityType == entityType && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));
    }
}
