using Platform.Application.Catalog.Variants;
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
