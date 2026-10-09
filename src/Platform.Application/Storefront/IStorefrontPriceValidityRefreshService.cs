namespace Platform.Application.Storefront;

/// <summary>
/// Enqueues storefront projection refresh requests for products whose projected price state
/// went stale purely because a price-list or price-list-entry validity boundary passed.
/// A pass is bounded, safe to run from several Worker instances at once, and enqueues one request
/// per product so the refresh pipeline can retry products independently.
/// </summary>
public interface IStorefrontPriceValidityRefreshService
{
    /// <param name="lookbackWindow">
    /// How far back boundaries are still considered. Bounds the scan and lets the pass recover
    /// boundaries that passed while the worker was not running.
    /// </param>
    /// <param name="maxProducts">Upper bound on products enqueued in one pass.</param>
    Task<int> EnqueueDuePriceBoundaryRefreshesAsync(
        TimeSpan lookbackWindow,
        int maxProducts,
        CancellationToken cancellationToken);
}
