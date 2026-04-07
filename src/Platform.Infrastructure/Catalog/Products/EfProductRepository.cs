using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Products;
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

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(x => x.ProductStatus)
            .Include(x => x.Translations)
            .OrderBy(x => x.ProductNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(x => x.ProductStatus)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
    }

    public async Task<Product?> GetByProductNumberAsync(string productNumber, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(x => x.ProductStatus)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.ProductNumber == productNumber, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }
}
