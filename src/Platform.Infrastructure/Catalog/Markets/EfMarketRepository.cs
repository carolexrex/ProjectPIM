using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Markets.Queries;
using Platform.Domain.Catalog.Markets;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Markets;

public sealed class EfMarketRepository : IMarketRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfMarketRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MarketListResult> ListAsync(ListMarketsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Markets
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search) || x.Code.Contains(query.Search) || x.Name.Contains(query.Search))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Currencies)
            .Include(x => x.Cultures)
            .Include(x => x.ProductAssignments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new MarketListResult(items, total);
    }

    public async Task<IReadOnlyList<Market>> ListLookupsAsync(ListMarketLookupsQuery query, CancellationToken cancellationToken)
    {
        return await _dbContext.Markets
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search) || x.Code.Contains(query.Search) || x.Name.Contains(query.Search))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => string.IsNullOrWhiteSpace(query.CurrencyCode)
                || x.Currencies.Any(currency => currency.CurrencyCode == query.CurrencyCode))
            .OrderBy(x => x.Code)
            .Include(x => x.Currencies)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Market>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Markets
            .AsNoTracking()
            .Where(x => x.Status == "Active")
            .OrderBy(x => x.Code)
            .Include(x => x.Currencies)
            .Include(x => x.Cultures)
            .Include(x => x.ProductAssignments)
            .ToListAsync(cancellationToken);
    }

    public async Task<Market?> GetByIdAsync(Guid marketId, CancellationToken cancellationToken)
    {
        return await _dbContext.Markets
            .Include(x => x.Currencies)
            .Include(x => x.Cultures)
            .Include(x => x.ProductAssignments)
            .FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);
    }

    public async Task<IReadOnlyList<Market>> GetByIdsAsync(IReadOnlyCollection<Guid> marketIds, CancellationToken cancellationToken)
    {
        if (marketIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Markets
            .AsNoTracking()
            .Include(x => x.Currencies)
            .Include(x => x.Cultures)
            .Include(x => x.ProductAssignments)
            .Where(x => marketIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Market?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Markets
            .Include(x => x.Currencies)
            .Include(x => x.Cultures)
            .Include(x => x.ProductAssignments)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task AddAsync(Market market, CancellationToken cancellationToken)
    {
        await _dbContext.Markets.AddAsync(market, cancellationToken);
    }

    private static IQueryable<Market> ApplySorting(IQueryable<Market> markets, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => markets.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => markets.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-name" => markets.OrderByDescending(x => x.Name).ThenBy(x => x.Code),
            "name" => markets.OrderBy(x => x.Name).ThenBy(x => x.Code),
            "-code" => markets.OrderByDescending(x => x.Code),
            _ => markets.OrderBy(x => x.Code)
        };
    }
}
