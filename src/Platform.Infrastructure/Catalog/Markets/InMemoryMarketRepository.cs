using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Markets.Queries;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Catalog.Markets;

public sealed class InMemoryMarketRepository : IMarketRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryMarketRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<MarketListResult> ListAsync(ListMarketsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
            _store.Markets.Values
                .Where(x => string.IsNullOrWhiteSpace(query.Search)
                    || x.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(query.Status)
                    || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase)),
            query.Sort)
            .ToList();

        return Task.FromResult(new MarketListResult(filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), filtered.Count));
    }

    public Task<IReadOnlyList<Market>> ListLookupsAsync(ListMarketLookupsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Market> items = _store.Markets.Values
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.Status)
                || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.CurrencyCode)
                || x.Currencies.Any(currency => string.Equals(currency.CurrencyCode, query.CurrencyCode, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<Market>> ListActiveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Market> items = _store.Markets.Values
            .Where(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<Market?> GetByIdAsync(Guid marketId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Markets.TryGetValue(marketId, out var market) ? market : null);
    }

    public Task<IReadOnlyList<Market>> GetByIdsAsync(IReadOnlyCollection<Guid> marketIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Market> items = marketIds.Where(id => _store.Markets.ContainsKey(id)).Select(id => _store.Markets[id]).ToList();
        return Task.FromResult(items);
    }

    public Task<Market?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Markets.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));
    }

    public Task AddAsync(Market market, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Markets[market.Id] = market;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<Market> ApplySorting(IEnumerable<Market> markets, string? sort)
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
