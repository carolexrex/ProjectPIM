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

    public async Task<ProductStatusDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductStatusDefinitions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
