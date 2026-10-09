using Microsoft.EntityFrameworkCore;
using Platform.Application.Storefront;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

/// <summary>
/// Proves the PostgreSQL and in-memory scan repositories agree on which stale products are
/// schedulable for every reservation state a scheduled request can be in.
/// </summary>
public sealed class StorefrontPriceValidityScanParityTests
{
    private const string DedupKeyPrefix = StorefrontPriceValidityRefreshService.DedupKeyPrefix;

    private static readonly DateTime NowUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BoundaryUtc = NowUtc.AddMinutes(-30);
    private static readonly DateTime ProjectedAtUtc = NowUtc.AddHours(-2);

    public enum ReservationState
    {
        None,
        Pending,
        Delayed,
        Abandoned,
        Published,
        OtherProducer
    }

    [Theory]
    // No reservation, or one that has been released by publishing: the product is schedulable.
    [InlineData(ReservationState.None, true)]
    [InlineData(ReservationState.Published, true)]
    // A message that belongs to a different producer must not suppress this pass.
    [InlineData(ReservationState.OtherProducer, true)]
    // Everything unpublished holds the key, abandonment included.
    [InlineData(ReservationState.Pending, false)]
    [InlineData(ReservationState.Delayed, false)]
    [InlineData(ReservationState.Abandoned, false)]
    public async Task BothProvidersAgreeOnReservationStates(ReservationState state, bool expectedSchedulable)
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var efCandidates = await ScanWithEfAsync(productId, variantId, state);
        var inMemoryCandidates = await ScanWithInMemoryAsync(productId, variantId, state);

        Assert.Equal(
            efCandidates.Select(x => (x.ProductId, x.BoundaryUtc)),
            inMemoryCandidates.Select(x => (x.ProductId, x.BoundaryUtc)));

        if (expectedSchedulable)
        {
            var candidate = Assert.Single(efCandidates);
            Assert.Equal(productId, candidate.ProductId);
            Assert.Equal(BoundaryUtc, candidate.BoundaryUtc);
        }
        else
        {
            Assert.Empty(efCandidates);
        }
    }

    private static async Task<IReadOnlyList<StorefrontPriceValidityCandidate>> ScanWithEfAsync(
        Guid productId,
        Guid variantId,
        ReservationState state)
    {
        await using var dbContext = StorefrontPriceValidityScanFixtures.CreateDbContext();

        var priceList = StorefrontPriceValidityScanFixtures.CreatePriceList("SE_BASE", null, null, NowUtc);
        priceList.UpsertEntry(null, "Variant", variantId, 1, 100m, null, BoundaryUtc, null, priceList.RowVersion);
        dbContext.PriceLists.Add(priceList);

        StorefrontPriceValidityScanFixtures.AddVariant(dbContext, productId, NowUtc, variantId);

        dbContext.StorefrontProductProjections.Add(
            StorefrontPriceValidityScanFixtures.CreateProjection(productId, "sv-SE", ProjectedAtUtc));

        var message = CreateReservation(productId, state);
        if (message is not null)
        {
            dbContext.OutboxMessages.Add(message);
        }

        await dbContext.SaveChangesAsync();

        return await new EfStorefrontPriceValidityScanRepository(dbContext).ListStaleProductsWithPassedBoundariesAsync(
            NowUtc.AddHours(-1),
            NowUtc,
            DedupKeyPrefix,
            10,
            CancellationToken.None);
    }

    private static Task<IReadOnlyList<StorefrontPriceValidityCandidate>> ScanWithInMemoryAsync(
        Guid productId,
        Guid variantId,
        ReservationState state)
    {
        var store = new InMemoryCatalogStore();

        var priceList = StorefrontPriceValidityScanFixtures.CreatePriceList("SE_PARITY", null, null, NowUtc);
        priceList.UpsertEntry(null, "Variant", variantId, 1, 100m, null, BoundaryUtc, null, priceList.RowVersion);
        store.PriceLists[priceList.Id] = priceList;

        var variant = StorefrontPriceValidityScanFixtures.CreateVariant(productId, variantId, NowUtc);
        store.Variants[variant.Id] = variant;

        var projection = StorefrontPriceValidityScanFixtures.CreateProjection(productId, "sv-SE", ProjectedAtUtc);
        store.StorefrontProductProjections[projection.Id] = projection;

        var message = CreateReservation(productId, state);
        if (message is not null)
        {
            store.OutboxMessages[message.Id] = message;
        }

        return new InMemoryStorefrontPriceValidityScanRepository(store).ListStaleProductsWithPassedBoundariesAsync(
            NowUtc.AddHours(-1),
            NowUtc,
            DedupKeyPrefix,
            10,
            CancellationToken.None);
    }

    private static OutboxMessage? CreateReservation(Guid productId, ReservationState state)
    {
        if (state == ReservationState.None)
        {
            return null;
        }

        var dedupKey = state == ReservationState.OtherProducer
            ? $"some-other-producer:{productId:N}"
            : StorefrontPriceValidityRefreshService.BuildDedupKey(productId);

        var message = new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            "StorefrontProductProjection",
            productId,
            "{}",
            NowUtc.AddMinutes(-10),
            dedupKey);

        switch (state)
        {
            case ReservationState.Delayed:
                message.MarkProcessingRetry("boom", NowUtc.AddHours(1), message.RowVersion);
                break;
            case ReservationState.Abandoned:
                message.MarkProcessingAbandoned("boom", message.RowVersion);
                break;
            case ReservationState.Published:
                message.MarkPublished(message.RowVersion);
                break;
        }

        return message;
    }
}
