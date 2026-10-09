using Microsoft.EntityFrameworkCore;
using Platform.Application.Storefront;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Storefront;

/// <summary>
/// PostgreSQL read-side query for the time-driven price-validity pass.
/// </summary>
/// <remarks>
/// Both the staleness join and the reservation anti-join are expressed here rather than in the
/// scheduler so the database returns a bounded page of products that still need to be scheduled.
/// Filtering before <c>ORDER BY</c> and <c>LIMIT</c> is what makes repeated passes complete: a
/// product that has been refreshed, or that already has an unpublished refresh request, drops out
/// of the result instead of holding a slot in the page forever.
///
/// The query spans pricing, catalog, and storefront tables because price-validity staleness is
/// inherently a read-model question. It stays a single storefront-owned query object instead of a
/// cross-module call chain, matching how <see cref="StorefrontProjectionBuilder"/> already reads
/// across modules.
/// </remarks>
public sealed class EfStorefrontPriceValidityScanRepository : IStorefrontPriceValidityScanRepository
{
    private const string ActiveStatus = "Active";
    private const string VariantTargetType = "Variant";

    private readonly PlatformDbContext _dbContext;

    public EfStorefrontPriceValidityScanRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<StorefrontPriceValidityCandidate>> ListStaleProductsWithPassedBoundariesAsync(
        DateTime windowStartExclusiveUtc,
        DateTime asOfInclusiveUtc,
        string reservedDedupKeyPrefix,
        int maxProducts,
        CancellationToken cancellationToken)
    {
        if (maxProducts <= 0 || windowStartExclusiveUtc >= asOfInclusiveUtc)
        {
            return [];
        }

        var rows = await BuildQuery(windowStartExclusiveUtc, asOfInclusiveUtc, reservedDedupKeyPrefix, maxProducts)
            .ToListAsync(cancellationToken);

        return rows;
    }

    /// <summary>
    /// Exposed so tests can assert the query translates to SQL on the PostgreSQL provider.
    /// </summary>
    internal IQueryable<StorefrontPriceValidityCandidate> BuildQuery(
        DateTime windowStartExclusiveUtc,
        DateTime asOfInclusiveUtc,
        string reservedDedupKeyPrefix,
        int maxProducts)
    {
        // One row per (active price list, variant-targeted entry, variant) with every validity
        // instant that could have transitioned. PriceListEntry has a shadow foreign key, so the
        // list is joined through the navigation rather than an explicit key comparison.
        var entryWindows =
            from priceList in _dbContext.PriceLists.AsNoTracking()
            where priceList.Status == ActiveStatus
            from entry in priceList.Entries
            where entry.TargetType == VariantTargetType && entry.TargetId != Guid.Empty
            join variant in _dbContext.Variants.AsNoTracking() on entry.TargetId equals variant.Id
            select new
            {
                variant.ProductId,
                ListValidFromUtc = priceList.ValidFromUtc,
                ListValidToUtc = priceList.ValidToUtc,
                EntryValidFromUtc = entry.ValidFromUtc,
                EntryValidToUtc = entry.ValidToUtc
            };

        // Unpivot the four instants into (product, boundary) rows. A list-level transition changes
        // eligibility for every entry the list carries, so it is emitted per entry.
        var boundaries = entryWindows
            .Where(x => x.EntryValidFromUtc.HasValue
                && x.EntryValidFromUtc.Value > windowStartExclusiveUtc
                && x.EntryValidFromUtc.Value <= asOfInclusiveUtc)
            .Select(x => new { x.ProductId, BoundaryUtc = x.EntryValidFromUtc!.Value })
            .Concat(entryWindows
                .Where(x => x.EntryValidToUtc.HasValue
                    && x.EntryValidToUtc.Value > windowStartExclusiveUtc
                    && x.EntryValidToUtc.Value <= asOfInclusiveUtc)
                .Select(x => new { x.ProductId, BoundaryUtc = x.EntryValidToUtc!.Value }))
            .Concat(entryWindows
                .Where(x => x.ListValidFromUtc.HasValue
                    && x.ListValidFromUtc.Value > windowStartExclusiveUtc
                    && x.ListValidFromUtc.Value <= asOfInclusiveUtc)
                .Select(x => new { x.ProductId, BoundaryUtc = x.ListValidFromUtc!.Value }))
            .Concat(entryWindows
                .Where(x => x.ListValidToUtc.HasValue
                    && x.ListValidToUtc.Value > windowStartExclusiveUtc
                    && x.ListValidToUtc.Value <= asOfInclusiveUtc)
                .Select(x => new { x.ProductId, BoundaryUtc = x.ListValidToUtc!.Value }));

        var oldestProjections = _dbContext.StorefrontProductProjections
            .AsNoTracking()
            .GroupBy(x => x.ProductId)
            .Select(x => new { ProductId = x.Key, OldestProjectedAtUtc = x.Min(y => y.ProjectedAtUtc) });

        // Products that already hold an unpublished scheduled request — pending, delayed, or
        // abandoned. The producer writes the product id as the message aggregate id, so the
        // reservation can be matched without recomputing the dedup key in SQL.
        var reservedProductIds = _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => !x.PublishedAtUtc.HasValue
                && x.DedupKey != null
                && x.DedupKey.StartsWith(reservedDedupKeyPrefix))
            .Select(x => x.AggregateId);

        // A product is stale when any passed boundary is at or after its oldest projection stamp.
        // Grouping after that filter still yields the latest in-window boundary, because the
        // largest boundary always qualifies whenever any boundary does. Reserved products are
        // removed before the grouping, ordering, and cap so they cannot consume the page.
        return boundaries
            .Join(
                oldestProjections,
                boundary => boundary.ProductId,
                projection => projection.ProductId,
                (boundary, projection) => new { boundary.ProductId, boundary.BoundaryUtc, projection.OldestProjectedAtUtc })
            .Where(x => x.OldestProjectedAtUtc <= x.BoundaryUtc)
            .Where(x => !reservedProductIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(x => new { ProductId = x.Key, BoundaryUtc = x.Max(y => y.BoundaryUtc) })
            .OrderBy(x => x.BoundaryUtc)
            .ThenBy(x => x.ProductId)
            .Take(maxProducts)
            .Select(x => new StorefrontPriceValidityCandidate(x.ProductId, x.BoundaryUtc));
    }
}
