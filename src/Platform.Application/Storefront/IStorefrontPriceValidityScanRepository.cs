namespace Platform.Application.Storefront;

/// <summary>
/// A product whose storefront projection predates a price-validity boundary that has already
/// passed. <paramref name="BoundaryUtc"/> is the latest such boundary inside the scanned window;
/// it decides staleness and orders the page so the longest-stale products are handled first.
/// </summary>
public sealed record StorefrontPriceValidityCandidate(Guid ProductId, DateTime BoundaryUtc);

/// <summary>
/// Read-side query for the time-driven storefront price-validity pass. Both staleness and existing
/// scheduling reservations are resolved by the persistence layer rather than in the scheduler, so a
/// bounded page always contains products that still need to be scheduled and no candidate can be
/// starved behind products that are already refreshed or already queued.
/// </summary>
public interface IStorefrontPriceValidityScanRepository
{
    /// <summary>
    /// Lists products whose oldest projection row is at or before the latest price-list or
    /// price-list-entry validity boundary that passed inside
    /// (<paramref name="windowStartExclusiveUtc"/>, <paramref name="asOfInclusiveUtc"/>], oldest
    /// boundary first, capped at <paramref name="maxProducts"/>.
    /// </summary>
    /// <param name="reservedDedupKeyPrefix">
    /// Products that already hold an unpublished outbox message whose dedup key starts with this
    /// prefix are excluded <em>before</em> ordering and capping, so pending, delayed, and abandoned
    /// work never consumes the page.
    /// </param>
    Task<IReadOnlyList<StorefrontPriceValidityCandidate>> ListStaleProductsWithPassedBoundariesAsync(
        DateTime windowStartExclusiveUtc,
        DateTime asOfInclusiveUtc,
        string reservedDedupKeyPrefix,
        int maxProducts,
        CancellationToken cancellationToken);
}
