using Platform.Domain.Catalog.Products;

namespace Platform.Application.Catalog.Products;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<Product?> GetByProductNumberAsync(string productNumber, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
}
