using Platform.Application.Storefront;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

/// <summary>
/// Scheduling guarantees of the time-driven price-validity pass: bounded forward progress,
/// idempotent enqueueing under concurrency, and per-product failure isolation.
/// </summary>
public sealed class StorefrontPriceValiditySchedulingTests
{
    private const int MaxPasses = 10;
    private const int ProductsPerPass = 2;

    // Deliberately far more already-refreshed products than one pass may enqueue: any design that
    // caps candidates before filtering for staleness fills every pass with these and never reaches
    // the stale tail behind them.
    private const int FreshProductCount = 24;
    private const int StaleProductCount = 4;

    private static readonly DateTime BaseUtc = StorefrontProjectionBuilderTests.ProjectionInstantUtc;
    private static readonly DateTime NowUtc = BaseUtc.AddHours(1);
    private static readonly TimeSpan Lookback = TimeSpan.FromHours(2);

    [Fact]
    public async Task RepeatedBoundedPasses_RefreshEveryStaleProduct_EvenWhenFresherProductsHoldTheEarliestBoundaries()
    {
        var context = new StorefrontPriceValidityTestContext();

        // The already-refreshed products carry the earliest boundaries, so they sort ahead of the
        // stale ones in every pass.
        var freshProductIds = Enumerable.Range(2, FreshProductCount)
            .Select(x => context.AddPricedProduct(x, BaseUtc.AddMinutes(x)))
            .ToList();
        var staleProductIds = Enumerable.Range(2 + FreshProductCount, StaleProductCount)
            .Select(x => context.AddPricedProduct(x, BaseUtc.AddMinutes(x)))
            .ToList();

        foreach (var productId in freshProductIds)
        {
            await context.ProjectAsync(NowUtc, productId);
        }

        foreach (var productId in staleProductIds)
        {
            await context.ProjectAsync(BaseUtc, productId);
        }

        var passes = 0;
        while (passes < MaxPasses)
        {
            var enqueued = await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
                Lookback,
                ProductsPerPass,
                CancellationToken.None);

            if (enqueued == 0)
            {
                break;
            }

            Assert.Equal(ProductsPerPass, enqueued);
            passes++;
            await context.CreateProcessor(NowUtc).ExecutePendingAsync(ProductsPerPass, CancellationToken.None);
        }

        Assert.Equal(StaleProductCount / ProductsPerPass, passes);
        Assert.All(staleProductIds, x => Assert.Equal(1299m, context.ProjectedPrice(x)));

        var enqueuedProductIds = context.RefreshMessages()
            .Select(StorefrontPriceValidityTestContext.ReadSingleProductId)
            .ToList();
        Assert.Equal(staleProductIds.Count, enqueuedProductIds.Count);
        Assert.Equal(staleProductIds.OrderBy(x => x), enqueuedProductIds.OrderBy(x => x));
    }

    [Fact]
    public async Task RepeatedScansBeforeProcessing_LeaveOneOpenRequestPerProductAndBoundary()
    {
        var context = await CreateStaleSeededProductAsync();

        Assert.Equal(1, await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));
        Assert.Equal(0, await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));
        Assert.Equal(0, await context.CreateScheduler(NowUtc.AddMinutes(5)).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));

        var message = Assert.Single(context.OpenRefreshMessages());
        Assert.Equal(context.SeededProductId, StorefrontPriceValidityTestContext.ReadSingleProductId(message));
    }

    [Fact]
    public async Task ScanAfterDelayedRetry_DoesNotCreateAMessageThatBypassesTheDelay()
    {
        var context = await CreateStaleSeededProductAsync();
        await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None);

        var failingRefreshService = new FailingStorefrontProjectionRefreshService(
            context.CreateRefreshService(NowUtc),
            context.SeededProductId);
        Assert.Equal(0, await context.CreateProcessor(NowUtc, failingRefreshService).ExecutePendingAsync(10, CancellationToken.None));

        var delayed = Assert.Single(context.OpenRefreshMessages());
        Assert.Equal(1, delayed.ProcessingAttemptCount);
        Assert.True(delayed.NextProcessingAttemptAtUtc > DateTime.UtcNow);

        // A further boundary passes while the retry is still waiting. The pending request will
        // rebuild the product at whatever instant it eventually runs, so it must not gain a second
        // message that would bypass the delay.
        context.SetSeededProductEntryWindow(BaseUtc.AddMinutes(30), NowUtc.AddMinutes(5));

        Assert.Equal(0, await context.CreateScheduler(NowUtc.AddMinutes(10)).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));

        var stillDelayed = Assert.Single(context.OpenRefreshMessages());
        Assert.Equal(delayed.Id, stillDelayed.Id);
        Assert.Equal(1, stillDelayed.ProcessingAttemptCount);
        Assert.Equal(delayed.NextProcessingAttemptAtUtc, stillDelayed.NextProcessingAttemptAtUtc);
    }

    [Fact]
    public async Task ConcurrentScheduling_IsRejectedByThePersistenceConstraint()
    {
        var context = await CreateStaleSeededProductAsync();

        // A competing Worker instance wins the race between this pass's scan and its insert.
        var racingPublisher = new RacingRefreshRequestPublisher(context);

        var enqueued = await context.CreateScheduler(NowUtc, racingPublisher).EnqueueDuePriceBoundaryRefreshesAsync(
            Lookback,
            10,
            CancellationToken.None);

        Assert.Equal(0, enqueued);
        Assert.True(racingPublisher.SawDuplicateRejection);
        var message = Assert.Single(context.OpenRefreshMessages());
        Assert.Equal(context.SeededProductId, StorefrontPriceValidityTestContext.ReadSingleProductId(message));
    }

    [Fact]
    public async Task OutboxRepository_RejectsASecondUnpublishedMessageForTheSameDedupKey()
    {
        var context = new StorefrontPriceValidityTestContext();
        var repository = new InMemoryOutboxMessageRepository(context.Store);
        const string dedupKey = "storefront-price-validity:duplicate";

        await repository.AddAsync(CreateMessage(dedupKey), CancellationToken.None);

        await Assert.ThrowsAsync<DuplicateOutboxDedupKeyException>(
            () => new InMemoryOutboxMessageRepository(context.Store).AddAsync(CreateMessage(dedupKey), CancellationToken.None));

        Assert.Single(context.Store.OutboxMessages.Values);
    }

    [Fact]
    public async Task AbandonedMessage_KeepsItsReservation()
    {
        var context = new StorefrontPriceValidityTestContext();
        var repository = new InMemoryOutboxMessageRepository(context.Store);
        const string dedupKey = "storefront-price-validity:abandoned";

        var abandoned = CreateMessage(dedupKey);
        await repository.AddAsync(abandoned, CancellationToken.None);
        abandoned.MarkProcessingAbandoned("boom", abandoned.RowVersion);

        Assert.True(abandoned.ReservesDedupKey);
        await Assert.ThrowsAsync<DuplicateOutboxDedupKeyException>(
            () => repository.AddAsync(CreateMessage(dedupKey), CancellationToken.None));
        Assert.Single(context.Store.OutboxMessages.Values);
    }

    [Fact]
    public async Task PublishedMessage_ReleasesItsReservation()
    {
        var context = new StorefrontPriceValidityTestContext();
        var repository = new InMemoryOutboxMessageRepository(context.Store);
        const string dedupKey = "storefront-price-validity:published";

        var published = CreateMessage(dedupKey);
        await repository.AddAsync(published, CancellationToken.None);
        published.MarkPublished(published.RowVersion);

        Assert.False(published.ReservesDedupKey);
        await repository.AddAsync(CreateMessage(dedupKey), CancellationToken.None);

        Assert.Equal(2, context.Store.OutboxMessages.Count);
    }

    [Fact]
    public async Task ReservedProductsDoNotConsumeTheCandidatePage()
    {
        var context = new StorefrontPriceValidityTestContext();
        var staleProductIds = Enumerable.Range(2, ProductsPerPass * 2)
            .Select(x => context.AddPricedProduct(x, BaseUtc.AddMinutes(x)))
            .ToList();

        foreach (var productId in staleProductIds)
        {
            await context.ProjectAsync(BaseUtc, productId);
        }

        // First page: the two earliest-boundary products.
        Assert.Equal(
            ProductsPerPass,
            await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, ProductsPerPass, CancellationToken.None));

        // They fail and are waiting out a retry delay, so they stay stale and keep the earliest
        // boundaries. They must not fill the next page.
        foreach (var message in context.OpenRefreshMessages())
        {
            message.MarkProcessingRetry("boom", NowUtc.AddHours(1), message.RowVersion);
        }

        Assert.Equal(
            ProductsPerPass,
            await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, ProductsPerPass, CancellationToken.None));

        var scheduledProductIds = context.RefreshMessages()
            .Select(StorefrontPriceValidityTestContext.ReadSingleProductId)
            .ToList();
        Assert.Equal(staleProductIds.OrderBy(x => x), scheduledProductIds.OrderBy(x => x));
    }

    [Fact]
    public async Task AbandonedRequest_IsNotReplacedByALaterScan()
    {
        var context = await CreateStaleSeededProductAsync();
        Assert.Equal(1, await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));

        var abandoned = context.AbandonScheduledRefresh();

        // The product is still stale, but its failure is recorded and must stay visible: scheduling
        // a replacement would quietly restart work an operator has not looked at yet.
        Assert.Equal(0, await context.CreateScheduler(NowUtc.AddMinutes(30)).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));

        var message = Assert.Single(context.RefreshMessages());
        Assert.Equal(abandoned.Id, message.Id);
        Assert.True(message.IsProcessingAbandoned);
        Assert.Empty(context.OpenRefreshMessages());
    }

    [Fact]
    public async Task ResettingAnAbandonedRequest_ReusesThatMessageWithoutADedupConflict()
    {
        var context = await CreateStaleSeededProductAsync();
        await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None);
        var abandoned = context.AbandonScheduledRefresh();

        // The admin reset workflow returns the existing message to service; because the message
        // never released its key there is nothing to collide with.
        abandoned.ResetProcessingForReplay(abandoned.RowVersion);

        var runnable = Assert.Single(context.OpenRefreshMessages());
        Assert.Equal(abandoned.Id, runnable.Id);
        Assert.Equal(0, runnable.ProcessingAttemptCount);
        Assert.False(runnable.IsProcessingAbandoned);

        Assert.Equal(1, await context.CreateProcessor(NowUtc).ExecutePendingAsync(10, CancellationToken.None));
        Assert.Single(context.RefreshMessages());
        Assert.Equal(1499m, context.ProjectedPrice(context.SeededProductId));
    }

    [Fact]
    public async Task PublishingAMessage_ReleasesTheKeyForTheNextBoundary()
    {
        var context = await CreateStaleSeededProductAsync();
        await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None);

        Assert.Equal(1, await context.CreateProcessor(NowUtc).ExecutePendingAsync(10, CancellationToken.None));
        Assert.Empty(context.ReservedRefreshMessages());
        Assert.Equal(0, await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));

        // A new boundary passes after the projection was rebuilt.
        context.SetSeededProductEntryWindow(BaseUtc.AddMinutes(30), NowUtc.AddMinutes(30));

        Assert.Equal(
            1,
            await context.CreateScheduler(NowUtc.AddMinutes(60)).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));

        Assert.Equal(2, context.RefreshMessages().Count);
        Assert.Equal(context.SeededProductId, StorefrontPriceValidityTestContext.ReadSingleProductId(Assert.Single(context.OpenRefreshMessages())));
    }

    [Fact]
    public async Task OneFailingProduct_DoesNotHoldBackTheProductsThatSucceed()
    {
        var context = new StorefrontPriceValidityTestContext();
        var boundaryUtc = BaseUtc.AddMinutes(30);
        var succeedingProductId = context.AddPricedProduct(2, boundaryUtc);
        var failingProductId = context.AddPricedProduct(3, boundaryUtc);
        await context.ProjectAsync(BaseUtc, succeedingProductId);
        await context.ProjectAsync(BaseUtc, failingProductId);

        Assert.Equal(2, await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));

        var failingRefreshService = new FailingStorefrontProjectionRefreshService(
            context.CreateRefreshService(NowUtc),
            failingProductId);
        var processed = await context.CreateProcessor(NowUtc, failingRefreshService).ExecutePendingAsync(10, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(1299m, context.ProjectedPrice(succeedingProductId));
        Assert.Null(context.ProjectedPrice(failingProductId));

        var open = Assert.Single(context.OpenRefreshMessages());
        Assert.Equal(failingProductId, StorefrontPriceValidityTestContext.ReadSingleProductId(open));
        Assert.Equal(1, open.ProcessingAttemptCount);
        Assert.True(open.NextProcessingAttemptAtUtc > DateTime.UtcNow);

        // Only the failing product stays scheduled; the refreshed one is no longer a candidate.
        Assert.Equal(0, await context.CreateScheduler(NowUtc).EnqueueDuePriceBoundaryRefreshesAsync(Lookback, 10, CancellationToken.None));
    }

    private static async Task<StorefrontPriceValidityTestContext> CreateStaleSeededProductAsync()
    {
        var context = new StorefrontPriceValidityTestContext();
        context.SetSeededProductEntryWindow(BaseUtc.AddMinutes(30), null);
        await context.ProjectAsync(BaseUtc);
        return context;
    }

    private static OutboxMessage CreateMessage(string dedupKey)
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            "StorefrontProductProjection",
            Guid.NewGuid(),
            "{}",
            DateTime.UtcNow,
            dedupKey);
    }

    private sealed class FailingStorefrontProjectionRefreshService : IStorefrontProjectionRefreshService
    {
        private readonly IStorefrontProjectionRefreshService _inner;
        private readonly Guid _failingProductId;

        public FailingStorefrontProjectionRefreshService(IStorefrontProjectionRefreshService inner, Guid failingProductId)
        {
            _inner = inner;
            _failingProductId = failingProductId;
        }

        public Task RefreshProductAsync(Guid productId, CancellationToken cancellationToken)
        {
            return RefreshProductsAsync([productId], cancellationToken);
        }

        public Task RefreshProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
        {
            if (productIds.Contains(_failingProductId))
            {
                throw new InvalidOperationException($"Refresh failed for product {_failingProductId}.");
            }

            return _inner.RefreshProductsAsync(productIds, cancellationToken);
        }

        public Task RebuildAllAsync(CancellationToken cancellationToken)
        {
            return _inner.RebuildAllAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Inserts the same dedup key from a second persistence session immediately before the pass
    /// commits its own message, which is exactly what two Worker instances scanning together do.
    /// </summary>
    private sealed class RacingRefreshRequestPublisher : IStorefrontProjectionRefreshRequestPublisher
    {
        private readonly StorefrontPriceValidityTestContext _context;

        public RacingRefreshRequestPublisher(StorefrontPriceValidityTestContext context)
        {
            _context = context;
        }

        public bool SawDuplicateRejection { get; private set; }

        public async Task EnqueueDeduplicatedProductRefreshAsync(
            Guid productId,
            string reason,
            string dedupKey,
            CancellationToken cancellationToken)
        {
            await _context.CreatePublisher().EnqueueDeduplicatedProductRefreshAsync(productId, reason, dedupKey, cancellationToken);

            try
            {
                await _context.CreatePublisher().EnqueueDeduplicatedProductRefreshAsync(productId, reason, dedupKey, cancellationToken);
            }
            catch (DuplicateOutboxDedupKeyException)
            {
                SawDuplicateRejection = true;
                throw;
            }
        }

        public Task EnqueueProductRefreshAsync(Guid productId, string reason, CancellationToken cancellationToken)
        {
            return _context.CreatePublisher().EnqueueProductRefreshAsync(productId, reason, cancellationToken);
        }

        public Task EnqueueVariantRefreshAsync(Guid variantId, string reason, CancellationToken cancellationToken)
        {
            return _context.CreatePublisher().EnqueueVariantRefreshAsync(variantId, reason, cancellationToken);
        }

        public Task EnqueueVariantsRefreshAsync(IReadOnlyCollection<Guid> variantIds, string reason, CancellationToken cancellationToken)
        {
            return _context.CreatePublisher().EnqueueVariantsRefreshAsync(variantIds, reason, cancellationToken);
        }
    }
}
