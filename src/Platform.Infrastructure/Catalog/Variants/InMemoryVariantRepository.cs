using Platform.Application.Catalog.Variants;
using Platform.Application.Catalog.Variants.Queries;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Catalog.Variants;

public sealed class InMemoryVariantRepository : IVariantRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryVariantRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<Variant>> ListByProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var items = _store.Variants.Values.Where(x => x.ProductId == productId).ToList();
        return Task.FromResult<IReadOnlyList<Variant>>(items);
    }

    public Task<IReadOnlyList<Variant>> ListLookupsAsync(ListVariantLookupsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Variant> items = _store.Variants.Values
            .Where(variant => string.IsNullOrWhiteSpace(query.Search)
                || variant.Sku.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || (variant.Ean?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (variant.Mpn?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (variant.Barcode?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ?? false))
            .Where(variant => string.IsNullOrWhiteSpace(query.Status)
                || string.Equals(variant.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(variant => query.ProductId is null || variant.ProductId == query.ProductId)
            .OrderBy(x => x.Sku, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<Variant>> GetByIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Variant> items = variantIds.Where(id => _store.Variants.ContainsKey(id)).Select(id => _store.Variants[id]).ToList();
        return Task.FromResult(items);
    }

    public Task<Variant?> GetByIdAsync(Guid variantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Variants.TryGetValue(variantId, out var variant) ? variant : null);
    }

    public Task<Variant?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var variant = _store.Variants.Values.FirstOrDefault(x =>
            string.Equals(x.Sku, sku, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(variant);
    }

    public Task AddAsync(Variant variant, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Variants[variant.Id] = variant;
        return Task.CompletedTask;
    }
}
