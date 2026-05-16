using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Products.Queries;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryProductRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<ProductListResult> ListAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
                _store.Products.Values
                    .Where(product => string.IsNullOrWhiteSpace(query.Search)
                        || product.ProductNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                        || product.Translations.Any(t => t.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
                    .Where(product => string.IsNullOrWhiteSpace(query.Status)
                        || string.Equals(product.Status, query.Status, StringComparison.OrdinalIgnoreCase))
                    .Where(product => string.IsNullOrWhiteSpace(query.ProductStatusCode)
                        || string.Equals(product.ProductStatus.Code, query.ProductStatusCode, StringComparison.OrdinalIgnoreCase))
                    .Where(product => query.BrandId is null || product.BrandId == query.BrandId)
                    .Where(product => query.HasVariants is null || product.HasVariants == query.HasVariants),
                query.Sort)
            .ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new ProductListResult(items, filtered.Count));
    }

    public Task<IReadOnlyList<Product>> ListForExportAsync(
        string? search,
        string? status,
        string? productStatusCode,
        Guid? brandId,
        bool? hasVariants,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Product> items = ApplySorting(
                _store.Products.Values
                    .Where(product => string.IsNullOrWhiteSpace(search)
                        || product.ProductNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || product.Translations.Any(t => t.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .Where(product => string.IsNullOrWhiteSpace(status)
                        || string.Equals(product.Status, status, StringComparison.OrdinalIgnoreCase))
                    .Where(product => string.IsNullOrWhiteSpace(productStatusCode)
                        || string.Equals(product.ProductStatus.Code, productStatusCode, StringComparison.OrdinalIgnoreCase))
                    .Where(product => brandId is null || product.BrandId == brandId)
                    .Where(product => hasVariants is null || product.HasVariants == hasVariants),
                "productnumber")
            .ToList();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<Product>> ListLookupsAsync(ListProductLookupsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Product> items = _store.Products.Values
            .Where(product => string.IsNullOrWhiteSpace(query.Search)
                || product.ProductNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || product.Translations.Any(t => t.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
            .Where(product => string.IsNullOrWhiteSpace(query.Status)
                || string.Equals(product.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(product => query.HasVariants is null || product.HasVariants == query.HasVariants)
            .Where(product => query.ExcludedProductId is null || product.Id != query.ExcludedProductId)
            .OrderBy(x => x.ProductNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<Guid>> ListIdsByBrandIdAsync(Guid brandId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Guid> ids = _store.Products.Values
            .Where(product => product.BrandId == brandId)
            .OrderBy(product => product.ProductNumber, StringComparer.OrdinalIgnoreCase)
            .Select(product => product.Id)
            .ToList();

        return Task.FromResult(ids);
    }

    public Task<IReadOnlyList<Product>> GetLookupByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Product> items = productIds
            .Distinct()
            .Where(id => _store.Products.ContainsKey(id))
            .Select(id => _store.Products[id])
            .ToList();

        return Task.FromResult(items);
    }

    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Products.TryGetValue(productId, out var product) ? product : null);
    }

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = productIds
            .Distinct()
            .Where(id => _store.Products.ContainsKey(id))
            .Select(id => _store.Products[id])
            .ToList();

        return Task.FromResult<IReadOnlyList<Product>>(items);
    }

    public Task<Product?> GetByProductNumberAsync(string productNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = _store.Products.Values.FirstOrDefault(x =>
            string.Equals(x.ProductNumber, productNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(product);
    }

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = _store.Products.Values.FirstOrDefault(x =>
            string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(product);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Products[product.Id] = product;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<Product> ApplySorting(IEnumerable<Product> products, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-createdatutc" => products.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.ProductNumber),
            "createdatutc" => products.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.ProductNumber),
            "-updatedatutc" => products.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.ProductNumber),
            "updatedatutc" => products.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.ProductNumber),
            "-productnumber" => products.OrderByDescending(x => x.ProductNumber),
            _ => products.OrderBy(x => x.ProductNumber)
        };
    }
}
