using Microsoft.EntityFrameworkCore;
using Platform.Application.Storefront;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Storefront;

public sealed class EfStorefrontProductProjectionRepository : IStorefrontProductProjectionRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfStorefrontProductProjectionRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<StorefrontProductProjection>> ListByContextAsync(
        string marketCode,
        string cultureCode,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StorefrontProductProjections
            .AsNoTracking()
            .Where(x => x.MarketCode == marketCode && x.CultureCode == cultureCode && x.CurrencyCode == currencyCode)
            .OrderBy(x => x.SortProductNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StorefrontProductProjection>> ListByContextAsync(
        Guid marketId,
        string cultureCode,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StorefrontProductProjections
            .AsNoTracking()
            .Where(x => x.MarketId == marketId && x.CultureCode == cultureCode && x.CurrencyCode == currencyCode)
            .OrderBy(x => x.SortProductNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StorefrontProductProjection>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.StorefrontProductProjections
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.MarketCode)
            .ThenBy(x => x.CultureCode)
            .ThenBy(x => x.CurrencyCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<StorefrontProductProjection?> GetBySlugAsync(
        string marketCode,
        string cultureCode,
        string currencyCode,
        string slug,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StorefrontProductProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.MarketCode == marketCode
                    && x.CultureCode == cultureCode
                    && x.CurrencyCode == currencyCode
                    && x.Slug == slug,
                cancellationToken);
    }

    public async Task<StorefrontProductProjection?> GetByProductNumberAsync(
        string marketCode,
        string cultureCode,
        string currencyCode,
        string productNumber,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StorefrontProductProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.MarketCode == marketCode
                    && x.CultureCode == cultureCode
                    && x.CurrencyCode == currencyCode
                    && x.ProductNumber == productNumber,
                cancellationToken);
    }

    public async Task ReplaceForProductAsync(Guid productId, IReadOnlyCollection<StorefrontProductProjection> projections, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.StorefrontProductProjections
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.StorefrontProductProjections.RemoveRange(existing);
        }

        if (projections.Count > 0)
        {
            await _dbContext.StorefrontProductProjections.AddRangeAsync(projections, cancellationToken);
        }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.StorefrontProductProjections.ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _dbContext.StorefrontProductProjections.RemoveRange(existing);
        }
    }
}
