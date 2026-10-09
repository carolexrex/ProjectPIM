using Platform.Application.Storefront;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Storefront;

/// <summary>
/// In-memory counterpart of <see cref="EfStorefrontPriceValidityScanRepository"/>. It applies the
/// same window, staleness, reservation, ordering, and cap rules so the development provider
/// schedules exactly what PostgreSQL would.
/// </summary>
public sealed class InMemoryStorefrontPriceValidityScanRepository : IStorefrontPriceValidityScanRepository
{
    private const string ActiveStatus = "Active";
    private const string VariantTargetType = "Variant";

    private readonly InMemoryCatalogStore _store;

    public InMemoryStorefrontPriceValidityScanRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<StorefrontPriceValidityCandidate>> ListStaleProductsWithPassedBoundariesAsync(
        DateTime windowStartExclusiveUtc,
        DateTime asOfInclusiveUtc,
        string reservedDedupKeyPrefix,
        int maxProducts,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxProducts <= 0 || windowStartExclusiveUtc >= asOfInclusiveUtc)
        {
            return Task.FromResult<IReadOnlyList<StorefrontPriceValidityCandidate>>([]);
        }

        var latestBoundaryByProductId = new Dictionary<Guid, DateTime>();

        foreach (var priceList in _store.PriceLists.Values
            .Where(x => string.Equals(x.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase)))
        {
            // A list-level transition changes eligibility for every entry the list carries.
            var listBoundaries = new[] { priceList.ValidFromUtc, priceList.ValidToUtc }
                .Where(x => IsInWindow(x, windowStartExclusiveUtc, asOfInclusiveUtc))
                .Select(x => x!.Value)
                .ToList();

            foreach (var entry in priceList.Entries.Where(x =>
                string.Equals(x.TargetType, VariantTargetType, StringComparison.OrdinalIgnoreCase)
                && x.TargetId != Guid.Empty))
            {
                if (!_store.Variants.TryGetValue(entry.TargetId, out var variant) || variant.ProductId == Guid.Empty)
                {
                    continue;
                }

                var boundaries = listBoundaries.Concat(
                    new[] { entry.ValidFromUtc, entry.ValidToUtc }
                        .Where(x => IsInWindow(x, windowStartExclusiveUtc, asOfInclusiveUtc))
                        .Select(x => x!.Value));

                foreach (var boundary in boundaries)
                {
                    if (!latestBoundaryByProductId.TryGetValue(variant.ProductId, out var existing) || boundary > existing)
                    {
                        latestBoundaryByProductId[variant.ProductId] = boundary;
                    }
                }
            }
        }

        var oldestProjectedAtUtcByProductId = _store.StorefrontProductProjections.Values
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.Min(y => y.ProjectedAtUtc));

        // Products that already hold an unpublished scheduled request — pending, delayed, or
        // abandoned — matched through the message aggregate id, as the EF query does.
        var reservedProductIds = _store.OutboxMessages.Values
            .Where(x => x.ReservesDedupKey && x.DedupKey!.StartsWith(reservedDedupKeyPrefix, StringComparison.Ordinal))
            .Select(x => x.AggregateId)
            .ToHashSet();

        IReadOnlyList<StorefrontPriceValidityCandidate> candidates = latestBoundaryByProductId
            .Where(x => oldestProjectedAtUtcByProductId.TryGetValue(x.Key, out var projectedAtUtc)
                && projectedAtUtc <= x.Value)
            .Where(x => !reservedProductIds.Contains(x.Key))
            .Select(x => new StorefrontPriceValidityCandidate(x.Key, x.Value))
            .OrderBy(x => x.BoundaryUtc)
            .ThenBy(x => x.ProductId)
            .Take(maxProducts)
            .ToList();

        return Task.FromResult(candidates);
    }

    private static bool IsInWindow(DateTime? instantUtc, DateTime windowStartExclusiveUtc, DateTime asOfInclusiveUtc)
    {
        return instantUtc.HasValue
            && instantUtc.Value > windowStartExclusiveUtc
            && instantUtc.Value <= asOfInclusiveUtc;
    }
}
