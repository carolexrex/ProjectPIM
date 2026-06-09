using Platform.Application.Storefront;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Storefront;

public sealed class InMemoryStorefrontProductProjectionRepository : IStorefrontProductProjectionRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryStorefrontProductProjectionRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<StorefrontProductProjection>> ListByContextAsync(
        string marketCode,
        string cultureCode,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<StorefrontProductProjection> items = _store.StorefrontProductProjections.Values
            .Where(x =>
                string.Equals(x.MarketCode, marketCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortProductNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<StorefrontProductProjection>> ListByContextAsync(
        Guid marketId,
        string cultureCode,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<StorefrontProductProjection> items = _store.StorefrontProductProjections.Values
            .Where(x =>
                x.MarketId == marketId
                && string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortProductNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<StorefrontProductProjection>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<StorefrontProductProjection> items = _store.StorefrontProductProjections.Values
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.MarketCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.CultureCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(items);
    }

    public Task<StorefrontProductProjection?> GetBySlugAsync(
        string marketCode,
        string cultureCode,
        string currencyCode,
        string slug,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var item = _store.StorefrontProductProjections.Values.FirstOrDefault(x =>
            string.Equals(x.MarketCode, marketCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(item);
    }

    public Task<StorefrontProductProjection?> GetByProductNumberAsync(
        string marketCode,
        string cultureCode,
        string currencyCode,
        string productNumber,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var item = _store.StorefrontProductProjections.Values.FirstOrDefault(x =>
            string.Equals(x.MarketCode, marketCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ProductNumber, productNumber, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(item);
    }

    public Task ReplaceForProductAsync(Guid productId, IReadOnlyCollection<StorefrontProductProjection> projections, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingIds = _store.StorefrontProductProjections.Values
            .Where(x => x.ProductId == productId)
            .Select(x => x.Id)
            .ToList();

        foreach (var id in existingIds)
        {
            _store.StorefrontProductProjections.TryRemove(id, out _);
        }

        foreach (var projection in projections)
        {
            _store.StorefrontProductProjections[projection.Id] = projection;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.StorefrontProductProjections.Clear();
        return Task.CompletedTask;
    }
}
