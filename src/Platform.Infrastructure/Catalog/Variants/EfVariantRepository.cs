using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Variants;
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
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Sku)
            .ToListAsync(cancellationToken);
    }

    public async Task<Variant?> GetByIdAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Variants
            .Include(x => x.ProductStatus)
            .Include(x => x.AttributeValues)
            .FirstOrDefaultAsync(x => x.Id == variantId, cancellationToken);
    }

    public async Task<Variant?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        return await _dbContext.Variants
            .Include(x => x.ProductStatus)
            .Include(x => x.AttributeValues)
            .FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);
    }

    public async Task AddAsync(Variant variant, CancellationToken cancellationToken)
    {
        await _dbContext.Variants.AddAsync(variant, cancellationToken);
    }
}
