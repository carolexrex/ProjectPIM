using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Products.Queries;
using Platform.Domain.Catalog.Products;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class EfProductRepository : IProductRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfProductRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductListResult> ListAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Products
            .AsNoTracking()
            .Where(product => string.IsNullOrWhiteSpace(query.Search)
                || product.ProductNumber.Contains(query.Search)
                || product.Translations.Any(t => t.Name.Contains(query.Search)))
            .Where(product => string.IsNullOrWhiteSpace(query.Status) || product.Status == query.Status)
            .Where(product => string.IsNullOrWhiteSpace(query.ProductStatusCode) || product.ProductStatus.Code == query.ProductStatusCode)
            .Where(product => query.BrandId == null || product.BrandId == query.BrandId)
            .Where(product => query.HasVariants == null || product.HasVariants == query.HasVariants);

        var total = await filteredQuery.CountAsync(cancellationToken);

        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.ProductStatus)
            .Include(x => x.CategoryAssignments)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Include(x => x.Relations)
            .Include(x => x.Translations)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ProductListResult(items, total);
    }

    public async Task<IReadOnlyList<Product>> ListForExportAsync(
        string? search,
        string? status,
        string? productStatusCode,
        Guid? brandId,
        bool? hasVariants,
        CancellationToken cancellationToken)
    {
        return await ApplySorting(
                _dbContext.Products
                    .AsNoTracking()
                    .Where(product => string.IsNullOrWhiteSpace(search)
                        || product.ProductNumber.Contains(search)
                        || product.Translations.Any(t => t.Name.Contains(search)))
                    .Where(product => string.IsNullOrWhiteSpace(status) || product.Status == status)
                    .Where(product => string.IsNullOrWhiteSpace(productStatusCode) || product.ProductStatus.Code == productStatusCode)
                    .Where(product => brandId == null || product.BrandId == brandId)
                    .Where(product => hasVariants == null || product.HasVariants == hasVariants),
                "productnumber")
            .Include(x => x.ProductStatus)
            .Include(x => x.CategoryAssignments)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Include(x => x.Relations)
            .Include(x => x.Translations)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListLookupsAsync(ListProductLookupsQuery query, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => string.IsNullOrWhiteSpace(query.Search)
                || product.ProductNumber.Contains(query.Search)
                || product.Translations.Any(t => t.Name.Contains(query.Search)))
            .Where(product => string.IsNullOrWhiteSpace(query.Status) || product.Status == query.Status)
            .Where(product => query.HasVariants == null || product.HasVariants == query.HasVariants)
            .Where(product => query.ExcludedProductId == null || product.Id != query.ExcludedProductId)
            .OrderBy(x => x.ProductNumber)
            .Include(x => x.Translations)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetLookupByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(x => x.ProductStatus)
            .Include(x => x.CategoryAssignments)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Include(x => x.Relations)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Products
            .Include(x => x.ProductStatus)
            .Include(x => x.CategoryAssignments)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Include(x => x.Relations)
            .Include(x => x.Translations)
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByProductNumberAsync(string productNumber, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(x => x.ProductStatus)
            .Include(x => x.CategoryAssignments)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Include(x => x.Relations)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.ProductNumber == productNumber, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(x => x.ProductStatus)
            .Include(x => x.CategoryAssignments)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Include(x => x.Relations)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> products, string? sort)
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
