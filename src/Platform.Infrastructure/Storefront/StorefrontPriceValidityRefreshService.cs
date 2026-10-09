using System.Globalization;
using Microsoft.Extensions.Logging;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Storefront;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Storefront;

/// <summary>
/// Time-driven storefront projection maintenance.
/// </summary>
/// <remarks>
/// Price-list and price-list-entry validity windows change eligibility without any write mutation,
/// so no outbox event fires when a scheduled price simply starts or expires. This pass asks the
/// read side for a bounded page of products whose projections predate a boundary that has already
/// passed, and enqueues one ordinary storefront projection refresh request per product.
///
/// Three properties matter:
/// <list type="bullet">
/// <item><description>Bounded forward progress — staleness and existing reservations are both
/// filtered inside the query, so every returned product still needs scheduling and neither
/// refreshed nor already-queued products can consume the page.</description></item>
/// <item><description>Idempotent scheduling — the dedup key is derived from the product, and
/// persistence rejects a second unpublished message for the same key, so re-scanning before the
/// refresh runs (or a second Worker instance scanning concurrently) cannot stack duplicates or
/// bypass a delayed retry. Abandoned work keeps its reservation and returns through the admin reset
/// workflow rather than being rescheduled automatically.</description></item>
/// <item><description>Failure isolation — one message per product lets the refresh processor retry
/// a failing product without holding back the products that succeeded.</description></item>
/// </list>
/// </remarks>
public sealed class StorefrontPriceValidityRefreshService : IStorefrontPriceValidityRefreshService
{
    public const string RefreshReason = "PriceValidityBoundaryPassed";

    /// <summary>
    /// Prefix shared by every dedup key this pass writes. The scan query uses it to recognise the
    /// reservations that belong to this producer.
    /// </summary>
    public const string DedupKeyPrefix = "storefront-price-validity:";

    private readonly IStorefrontPriceValidityScanRepository _scanRepository;
    private readonly IStorefrontProjectionRefreshRequestPublisher _refreshRequestPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StorefrontPriceValidityRefreshService> _logger;

    public StorefrontPriceValidityRefreshService(
        IStorefrontPriceValidityScanRepository scanRepository,
        IStorefrontProjectionRefreshRequestPublisher refreshRequestPublisher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<StorefrontPriceValidityRefreshService> logger)
    {
        _scanRepository = scanRepository;
        _refreshRequestPublisher = refreshRequestPublisher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Scheduling identity for one product's outstanding price-validity refresh.
    /// </summary>
    /// <remarks>
    /// The key is deliberately product-scoped rather than product-and-boundary-scoped. A refresh
    /// rebuilds the product at the instant it runs, so one unpublished request already covers every
    /// boundary that has passed for that product — including boundaries that pass while the request
    /// waits out a retry delay. Keying on the boundary as well would let those later boundaries
    /// each add another message for the same product.
    /// </remarks>
    public static string BuildDedupKey(Guid productId)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{DedupKeyPrefix}{productId:N}");
    }

    public async Task<int> EnqueueDuePriceBoundaryRefreshesAsync(
        TimeSpan lookbackWindow,
        int maxProducts,
        CancellationToken cancellationToken)
    {
        if (maxProducts <= 0 || lookbackWindow <= TimeSpan.Zero)
        {
            return 0;
        }

        var asOfUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStartUtc = asOfUtc - lookbackWindow;

        // The scan already excluded products holding an unpublished request, so every candidate
        // needs scheduling. The unique index remains the guard against a concurrent writer.
        var candidates = await _scanRepository.ListStaleProductsWithPassedBoundariesAsync(
            windowStartUtc,
            asOfUtc,
            DedupKeyPrefix,
            maxProducts,
            cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var enqueued = 0;

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // One message per product, saved on its own, so the refresh processor can retry a
                // failing product independently of the rest of the page.
                await _refreshRequestPublisher.EnqueueDeduplicatedProductRefreshAsync(
                    candidate.ProductId,
                    RefreshReason,
                    BuildDedupKey(candidate.ProductId),
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                enqueued++;
            }
            catch (DuplicateOutboxDedupKeyException exception)
            {
                // Another Worker instance scheduled this product first. The rejected insert may
                // still be tracked by the current persistence session, so the pass stops here and
                // lets the next scan continue from a clean scope; the remaining candidates stay
                // stale and are picked up then.
                _logger.LogDebug(
                    exception,
                    "Storefront price-validity refresh for product {ProductId} was already scheduled by another producer. Ending the pass.",
                    candidate.ProductId);
                break;
            }
        }

        if (enqueued == 0)
        {
            return 0;
        }

        _logger.LogInformation(
            "Enqueued storefront projection refresh for {ProductCount} product(s) after price validity boundaries passed between {WindowStartUtc:O} and {AsOfUtc:O}.",
            enqueued,
            windowStartUtc,
            asOfUtc);

        return enqueued;
    }
}
