using Platform.Domain.Catalog.Variants;

namespace Platform.Application.Catalog.Variants;

public interface IVariantRepository
{
    Task<IReadOnlyList<Variant>> ListByProductAsync(Guid productId, CancellationToken cancellationToken);
    Task<Variant?> GetByIdAsync(Guid variantId, CancellationToken cancellationToken);
    Task<Variant?> GetBySkuAsync(string sku, CancellationToken cancellationToken);
    Task AddAsync(Variant variant, CancellationToken cancellationToken);
}
