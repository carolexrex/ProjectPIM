using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

/// <summary>
/// Selection semantics of the time-driven price-validity scan: which products a pass picks up and
/// which it leaves alone.
/// </summary>
public sealed class StorefrontPriceValidityRefreshServiceTests
{
    private static readonly DateTime ProjectedAtUtc = StorefrontProjectionBuilderTests.ProjectionInstantUtc;

    [Fact]
    public async Task EnqueueDuePriceBoundaryRefreshesAsync_EnqueuesProductWhenEntryWindowOpenedAfterProjection()
    {
        var context = new StorefrontPriceValidityTestContext();
        var boundaryUtc = ProjectedAtUtc.AddMinutes(30);
        var nowUtc = ProjectedAtUtc.AddMinutes(60);
        context.SetSeededProductEntryWindow(boundaryUtc, null);
        await context.ProjectAsync(ProjectedAtUtc);

        var enqueued = await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
            TimeSpan.FromHours(2),
            10,
            CancellationToken.None);

        Assert.Equal(1, enqueued);
        var message = Assert.Single(context.RefreshMessages());
        Assert.Equal(context.SeededProductId, StorefrontPriceValidityTestContext.ReadSingleProductId(message));
        Assert.Equal(
            StorefrontPriceValidityRefreshService.RefreshReason,
            StorefrontPriceValidityTestContext.ReadPayload(message).Reason);
        Assert.Equal(StorefrontPriceValidityRefreshService.BuildDedupKey(context.SeededProductId), message.DedupKey);
    }

    [Fact]
    public async Task EnqueueDuePriceBoundaryRefreshesAsync_EnqueuesProductWhenEntryWindowClosedAfterProjection()
    {
        var context = new StorefrontPriceValidityTestContext();
        var boundaryUtc = ProjectedAtUtc.AddMinutes(30);
        var nowUtc = ProjectedAtUtc.AddMinutes(60);
        context.SetSeededProductEntryWindow(ProjectedAtUtc.AddHours(-1), boundaryUtc);
        await context.ProjectAsync(ProjectedAtUtc);

        var enqueued = await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
            TimeSpan.FromHours(2),
            10,
            CancellationToken.None);

        Assert.Equal(1, enqueued);
        Assert.Equal(
            context.SeededProductId,
            StorefrontPriceValidityTestContext.ReadSingleProductId(Assert.Single(context.RefreshMessages())));
    }

    [Fact]
    public async Task EnqueueDuePriceBoundaryRefreshesAsync_SkipsProductAlreadyProjectedAfterBoundary()
    {
        var context = new StorefrontPriceValidityTestContext();
        var boundaryUtc = ProjectedAtUtc.AddMinutes(30);
        var nowUtc = ProjectedAtUtc.AddMinutes(60);
        context.SetSeededProductEntryWindow(boundaryUtc, null);
        await context.ProjectAsync(nowUtc);

        var enqueued = await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
            TimeSpan.FromHours(2),
            10,
            CancellationToken.None);

        Assert.Equal(0, enqueued);
        Assert.Empty(context.RefreshMessages());
    }

    [Fact]
    public async Task EnqueueDuePriceBoundaryRefreshesAsync_IgnoresBoundariesOutsideLookbackWindow()
    {
        var context = new StorefrontPriceValidityTestContext();
        var boundaryUtc = ProjectedAtUtc.AddMinutes(30);
        var nowUtc = ProjectedAtUtc.AddMinutes(60);
        context.SetSeededProductEntryWindow(boundaryUtc, null);
        await context.ProjectAsync(ProjectedAtUtc);

        var enqueued = await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
            TimeSpan.FromMinutes(10),
            10,
            CancellationToken.None);

        Assert.Equal(0, enqueued);
        Assert.Empty(context.RefreshMessages());
    }

    [Fact]
    public async Task EnqueueDuePriceBoundaryRefreshesAsync_IgnoresBoundaryThatHasNotPassedYet()
    {
        var context = new StorefrontPriceValidityTestContext();
        var nowUtc = ProjectedAtUtc.AddMinutes(60);
        context.SetSeededProductEntryWindow(nowUtc.AddMinutes(30), null);
        await context.ProjectAsync(ProjectedAtUtc);

        var enqueued = await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
            TimeSpan.FromHours(2),
            10,
            CancellationToken.None);

        Assert.Equal(0, enqueued);
        Assert.Empty(context.RefreshMessages());
    }

    [Fact]
    public async Task EnqueueDuePriceBoundaryRefreshesAsync_BoundsEnqueuedProductsPerPass()
    {
        var context = new StorefrontPriceValidityTestContext();
        var boundaryUtc = ProjectedAtUtc.AddMinutes(30);
        var nowUtc = ProjectedAtUtc.AddMinutes(60);
        context.SetSeededProductEntryWindow(boundaryUtc, null);
        var secondProductId = context.AddPricedProduct(2, boundaryUtc);
        await context.ProjectAsync(ProjectedAtUtc);
        await context.ProjectAsync(ProjectedAtUtc, secondProductId);

        var enqueued = await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
            TimeSpan.FromHours(2),
            1,
            CancellationToken.None);

        Assert.Equal(1, enqueued);
        Assert.Single(context.RefreshMessages());
    }

    [Fact]
    public async Task EnqueuedRefreshRequest_RepricesProjectionThroughTheExistingRefreshPipeline()
    {
        var context = new StorefrontPriceValidityTestContext();
        var boundaryUtc = ProjectedAtUtc.AddMinutes(30);
        var nowUtc = ProjectedAtUtc.AddMinutes(60);
        context.SetSeededProductEntryWindow(boundaryUtc, null);
        await context.ProjectAsync(ProjectedAtUtc);

        Assert.Null(context.ProjectedPrice(context.SeededProductId));

        await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
            TimeSpan.FromHours(2),
            10,
            CancellationToken.None);

        Assert.Equal(1, await context.CreateProcessor(nowUtc).ExecutePendingAsync(10, CancellationToken.None));
        Assert.Equal(1499m, context.ProjectedPrice(context.SeededProductId));

        // The pass is idempotent once the projection is newer than the boundary.
        Assert.Equal(
            0,
            await context.CreateScheduler(nowUtc).EnqueueDuePriceBoundaryRefreshesAsync(
                TimeSpan.FromHours(2),
                10,
                CancellationToken.None));
    }
}
