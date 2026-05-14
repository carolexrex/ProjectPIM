using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Pricing.Queries;
using Platform.Domain.Catalog.Pricing;

namespace Platform.Infrastructure.Catalog.Pricing;

public sealed class InMemoryPriceListRepository : IPriceListRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryPriceListRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<PriceListListResult> ListAsync(ListPriceListsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
            _store.PriceLists.Values
                .Where(x => string.IsNullOrWhiteSpace(query.Search)
                    || x.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(query.CurrencyCode)
                    || string.Equals(x.CurrencyCode, query.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(query.Status)
                    || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
                .Where(x => query.MarketId is null || x.MarketAssignments.Any(y => y.MarketId == query.MarketId)),
            query.Sort)
            .ToList();

        return Task.FromResult(new PriceListListResult(filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), filtered.Count));
    }

    public Task<PriceList?> GetByIdAsync(Guid priceListId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.PriceLists.TryGetValue(priceListId, out var priceList) ? priceList : null);
    }

    public Task<IReadOnlyList<PriceList>> ListActiveByMarketAsync(Guid marketId, string currencyCode, DateTime instantUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = _store.PriceLists.Values
            .Where(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Where(x => string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.ValidFromUtc.HasValue || x.ValidFromUtc.Value <= instantUtc)
            .Where(x => !x.ValidToUtc.HasValue || x.ValidToUtc.Value >= instantUtc)
            .Where(x => x.MarketAssignments.Any(y => y.MarketId == marketId))
            .OrderBy(x => x.MarketAssignments.First(y => y.MarketId == marketId).Priority)
            .ThenByDescending(x => x.MarketAssignments.First(y => y.MarketId == marketId).IsBasePriceList)
            .ThenBy(x => x.Code)
            .ToList();

        return Task.FromResult<IReadOnlyList<PriceList>>(items);
    }

    public Task<PriceList?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.PriceLists.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));
    }

    public Task AddAsync(PriceList priceList, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.PriceLists[priceList.Id] = priceList;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<PriceList> ApplySorting(IEnumerable<PriceList> priceLists, string? sort)
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
