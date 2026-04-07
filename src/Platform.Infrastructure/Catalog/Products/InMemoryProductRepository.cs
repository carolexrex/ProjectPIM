using Platform.Application.Catalog.Products;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryProductRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Product>>(_store.Products.Values.ToList());
    }

    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Products.TryGetValue(productId, out var product) ? product : null);
    }

    public Task<Product?> GetByProductNumberAsync(string productNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = _store.Products.Values.FirstOrDefault(x =>
            string.Equals(x.ProductNumber, productNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(product);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Products[product.Id] = product;
        return Task.CompletedTask;
    }
}
