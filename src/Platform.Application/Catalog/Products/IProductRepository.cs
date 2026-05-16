using Platform.Domain.Catalog.Products;
using Platform.Application.Catalog.Products.Queries;

namespace Platform.Application.Catalog.Products;

public interface IProductRepository
{
    Task<ProductListResult> ListAsync(ListProductsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> ListForExportAsync(string? search, string? status, string? productStatusCode, Guid? brandId, bool? hasVariants, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> ListLookupsAsync(ListProductLookupsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListIdsByBrandIdAsync(Guid brandId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListIdsByCategoryIdsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> GetLookupByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task<Product?> GetByProductNumberAsync(string productNumber, CancellationToken cancellationToken);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
}
