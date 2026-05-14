using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Pricing.Queries;
using Platform.Domain.Catalog.Pricing;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Pricing;

public sealed class EfPriceListRepository : IPriceListRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfPriceListRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PriceListListResult> ListAsync(ListPriceListsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search) || x.Code.Contains(query.Search) || x.Name.Contains(query.Search))
            .Where(x => string.IsNullOrWhiteSpace(query.CurrencyCode) || x.CurrencyCode == query.CurrencyCode)
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => !query.MarketId.HasValue || x.MarketAssignments.Any(y => y.MarketId == query.MarketId.Value));

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.MarketAssignments)
            .Include(x => x.Entries)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PriceListListResult(items, total);
    }

    public async Task<PriceList?> GetByIdAsync(Guid priceListId, CancellationToken cancellationToken)
    {
        return await _dbContext.PriceLists
            .Include(x => x.MarketAssignments)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == priceListId, cancellationToken);
    }

    public async Task<IReadOnlyList<PriceList>> ListActiveByMarketAsync(Guid marketId, string currencyCode, DateTime instantUtc, CancellationToken cancellationToken)
    {
        return await _dbContext.PriceLists
            .Include(x => x.MarketAssignments)
            .Include(x => x.Entries)
            .Where(x => x.Status == "Active")
            .Where(x => x.CurrencyCode == currencyCode)
            .Where(x => !x.ValidFromUtc.HasValue || x.ValidFromUtc.Value <= instantUtc)
            .Where(x => !x.ValidToUtc.HasValue || x.ValidToUtc.Value >= instantUtc)
            .Where(x => x.MarketAssignments.Any(y => y.MarketId == marketId))
            .OrderBy(x => x.MarketAssignments.First(y => y.MarketId == marketId).Priority)
            .ThenByDescending(x => x.MarketAssignments.First(y => y.MarketId == marketId).IsBasePriceList)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceList?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.PriceLists
            .Include(x => x.MarketAssignments)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task AddAsync(PriceList priceList, CancellationToken cancellationToken)
    {
        await _dbContext.PriceLists.AddAsync(priceList, cancellationToken);
    }

    private static IQueryable<PriceList> ApplySorting(IQueryable<PriceList> priceLists, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => priceLists.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => priceLists.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-currencycode" => priceLists.OrderByDescending(x => x.CurrencyCode).ThenBy(x => x.Code),
            "currencycode" => priceLists.OrderBy(x => x.CurrencyCode).ThenBy(x => x.Code),
            "-name" => priceLists.OrderByDescending(x => x.Name).ThenBy(x => x.Code),
            "name" => priceLists.OrderBy(x => x.Name).ThenBy(x => x.Code),
            "-code" => priceLists.OrderByDescending(x => x.Code),
            _ => priceLists.OrderBy(x => x.Code)
        };
    }
}
