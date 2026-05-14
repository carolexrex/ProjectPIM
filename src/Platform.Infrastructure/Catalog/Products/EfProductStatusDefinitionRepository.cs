using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Products;
using Platform.Domain.Catalog.Products;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Products;

public sealed class EfProductStatusDefinitionRepository : IProductStatusDefinitionRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfProductStatusDefinitionRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductStatusDefinition>> ListAsync(
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductStatusDefinitions
            .AsNoTracking()
            .Where(x => x.EntityType == entityType)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductStatusDefinition?> GetByIdAsync(
        Guid id,
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductStatusDefinitions
            .FirstOrDefaultAsync(x => x.Id == id && x.EntityType == entityType, cancellationToken);
    }

    public async Task<ProductStatusDefinition?> GetByCodeAsync(
        string code,
        ProductStatusEntityType entityType,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductStatusDefinitions
            .FirstOrDefaultAsync(x => x.Code == code && x.EntityType == entityType, cancellationToken);
    }
}
