using Platform.Domain.Catalog.Variants;
using Platform.Application.Catalog.Variants.Queries;

namespace Platform.Application.Catalog.Variants;

public interface IVariantRepository
{
    Task<IReadOnlyList<Variant>> ListByProductAsync(Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Variant>> ListLookupsAsync(ListVariantLookupsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Variant>> GetByIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken);
    Task<Variant?> GetByIdAsync(Guid variantId, CancellationToken cancellationToken);
    Task<Variant?> GetBySkuAsync(string sku, CancellationToken cancellationToken);
    Task AddAsync(Variant variant, CancellationToken cancellationToken);
}
