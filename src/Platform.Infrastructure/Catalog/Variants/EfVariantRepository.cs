using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Variants;
using Platform.Application.Catalog.Variants.Queries;
using Platform.Domain.Catalog.Variants;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Variants;

public sealed class EfVariantRepository : IVariantRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfVariantRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Variant>> ListByProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Variants
            .Include(x => x.ProductStatus)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Sku)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Variant>> ListLookupsAsync(ListVariantLookupsQuery query, CancellationToken cancellationToken)
    {
        return await _dbContext.Variants
            .AsNoTracking()
            .Where(variant => string.IsNullOrWhiteSpace(query.Search)
                || variant.Sku.Contains(query.Search)
                || (variant.Ean != null && variant.Ean.Contains(query.Search))
                || (variant.Mpn != null && variant.Mpn.Contains(query.Search))
                || (variant.Barcode != null && variant.Barcode.Contains(query.Search)))
            .Where(variant => string.IsNullOrWhiteSpace(query.Status) || variant.Status == query.Status)
            .Where(variant => query.ProductId == null || variant.ProductId == query.ProductId)
            .OrderBy(x => x.Sku)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Variant>> GetByIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Variants
            .AsNoTracking()
            .Include(x => x.ProductStatus)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .Where(x => variantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Variant?> GetByIdAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Variants
            .Include(x => x.ProductStatus)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == variantId, cancellationToken);
    }

    public async Task<Variant?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        return await _dbContext.Variants
            .Include(x => x.ProductStatus)
            .Include(x => x.AttributeValues)
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);
    }

    public async Task AddAsync(Variant variant, CancellationToken cancellationToken)
    {
        await _dbContext.Variants.AddAsync(variant, cancellationToken);
    }
}
