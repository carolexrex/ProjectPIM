using Microsoft.EntityFrameworkCore;
using Platform.Application.Storefront;
using Platform.Domain.Catalog.Pricing;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Persistence.Configurations.Integrations;
using Platform.Infrastructure.Security;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

/// <summary>
/// Guards the EF query shapes used by the time-driven price-validity refresh pass, including that
/// the scan query still translates on the PostgreSQL provider.
/// </summary>
public sealed class EfStorefrontPriceValidityQueryTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private const string DedupKeyPrefix = StorefrontPriceValidityRefreshService.DedupKeyPrefix;

    [Fact]
    public async Task ListStaleProductsWithPassedBoundariesAsync_ReturnsLatestBoundaryForStaleProduct()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        var variantId = AddVariant(dbContext, productId);
        var archivedProductId = Guid.NewGuid();
        var archivedVariantId = AddVariant(dbContext, archivedProductId);

        var priceList = CreatePriceList("SE_BASE", null, null);
        priceList.UpsertEntry(null, "Variant", variantId, 1, 100m, null, NowUtc.AddHours(-3), NowUtc.AddMinutes(-30), priceList.RowVersion);
        dbContext.PriceLists.Add(priceList);

        // Only active price lists schedule work.
        var archived = CreatePriceList("SE_ARCHIVED", null, null);
        archived.UpsertEntry(null, "Variant", archivedVariantId, 1, 300m, null, NowUtc.AddMinutes(-10), null, archived.RowVersion);
        archived.Archive();
        dbContext.PriceLists.Add(archived);

        dbContext.StorefrontProductProjections.Add(CreateProjection(productId, "sv-SE", NowUtc.AddHours(-2)));
        dbContext.StorefrontProductProjections.Add(CreateProjection(archivedProductId, "sv-SE", NowUtc.AddHours(-2)));
        await dbContext.SaveChangesAsync();

        var candidates = await CreateScanRepository(dbContext).ListStaleProductsWithPassedBoundariesAsync(
            NowUtc.AddHours(-1),
            NowUtc,
            DedupKeyPrefix,
            10,
            CancellationToken.None);

        var candidate = Assert.Single(candidates);
        Assert.Equal(productId, candidate.ProductId);
        Assert.Equal(NowUtc.AddMinutes(-30), candidate.BoundaryUtc);
    }

    [Fact]
    public async Task ListStaleProductsWithPassedBoundariesAsync_AppliesListLevelBoundaryToEveryEntry()
    {
        await using var dbContext = CreateDbContext();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();

        var priceList = CreatePriceList("SE_CAMPAIGN", null, NowUtc.AddMinutes(-5));
        priceList.UpsertEntry(null, "Variant", AddVariant(dbContext, firstProductId), 1, 100m, null, null, null, priceList.RowVersion);
        priceList.UpsertEntry(null, "Variant", AddVariant(dbContext, secondProductId), 1, 200m, null, null, null, priceList.RowVersion);
        dbContext.PriceLists.Add(priceList);

        dbContext.StorefrontProductProjections.Add(CreateProjection(firstProductId, "sv-SE", NowUtc.AddHours(-2)));
        dbContext.StorefrontProductProjections.Add(CreateProjection(secondProductId, "sv-SE", NowUtc.AddHours(-2)));
        await dbContext.SaveChangesAsync();

        var candidates = await CreateScanRepository(dbContext).ListStaleProductsWithPassedBoundariesAsync(
            NowUtc.AddHours(-1),
            NowUtc,
            DedupKeyPrefix,
            10,
            CancellationToken.None);

        Assert.Equal(2, candidates.Count);
        Assert.All(candidates, x => Assert.Equal(NowUtc.AddMinutes(-5), x.BoundaryUtc));
        Assert.Contains(candidates, x => x.ProductId == firstProductId);
        Assert.Contains(candidates, x => x.ProductId == secondProductId);
    }

    [Fact]
    public async Task ListStaleProductsWithPassedBoundariesAsync_ExcludesProductsProjectedAfterTheBoundary()
    {
        await using var dbContext = CreateDbContext();
        var staleProductId = Guid.NewGuid();
        var freshProductId = Guid.NewGuid();

        var priceList = CreatePriceList("SE_BASE", null, null);
        priceList.UpsertEntry(null, "Variant", AddVariant(dbContext, staleProductId), 1, 100m, null, NowUtc.AddMinutes(-20), null, priceList.RowVersion);
        priceList.UpsertEntry(null, "Variant", AddVariant(dbContext, freshProductId), 1, 200m, null, NowUtc.AddMinutes(-20), null, priceList.RowVersion);
        dbContext.PriceLists.Add(priceList);

        dbContext.StorefrontProductProjections.Add(CreateProjection(staleProductId, "sv-SE", NowUtc.AddMinutes(-30)));
        // Oldest row decides: one fresh culture row must not hide a stale one.
        dbContext.StorefrontProductProjections.Add(CreateProjection(freshProductId, "sv-SE", NowUtc.AddMinutes(-10)));
        dbContext.StorefrontProductProjections.Add(CreateProjection(freshProductId, "en-GB", NowUtc.AddMinutes(-5)));
        await dbContext.SaveChangesAsync();

        var candidates = await CreateScanRepository(dbContext).ListStaleProductsWithPassedBoundariesAsync(
            NowUtc.AddHours(-1),
            NowUtc,
            DedupKeyPrefix,
            10,
            CancellationToken.None);

        Assert.Equal(staleProductId, Assert.Single(candidates).ProductId);
    }

    [Fact]
    public void ListStaleProductsWithPassedBoundaries_TranslatesToPostgreSql()
    {
        // No connection is opened: ToQueryString only compiles the expression tree, which fails
        // loudly if any part of the scan query cannot be translated by Npgsql.
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql("Host=localhost;Database=platform_translation_check;Username=test;Password=test")
            .Options;

        using var dbContext = new PlatformDbContext(options, new SystemCurrentActorAccessor());

        var sql = new EfStorefrontPriceValidityScanRepository(dbContext)
            .BuildQuery(NowUtc.AddHours(-1), NowUtc, DedupKeyPrefix, 100)
            .ToQueryString();

        Assert.Contains("PriceListEntry", sql, StringComparison.Ordinal);
        Assert.Contains("StorefrontProductProjection", sql, StringComparison.Ordinal);
        Assert.Contains("OutboxMessage", sql, StringComparison.Ordinal);
        Assert.Contains("UNION ALL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);

        // The reservation anti-join and the staleness filter must both sit inside the query, ahead
        // of the ordering and the cap, or a full page of reserved products would starve the rest.
        var limitIndex = sql.LastIndexOf("LIMIT", StringComparison.OrdinalIgnoreCase);
        Assert.True(sql.IndexOf("OutboxMessage", StringComparison.Ordinal) < limitIndex);
        Assert.True(sql.LastIndexOf("ORDER BY", StringComparison.Ordinal) < limitIndex);
    }

    [Fact]
    public void OutboxMessage_HasPartialUniqueIndexOnUnpublishedDedupKeys()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql("Host=localhost;Database=platform_translation_check;Username=test;Password=test")
            .Options;

        using var dbContext = new PlatformDbContext(options, new SystemCurrentActorAccessor());

        var index = Assert.Single(
            dbContext.Model.FindEntityType(typeof(OutboxMessage))!.GetIndexes(),
            x => x.GetDatabaseName() == OutboxMessageConfiguration.ReservedDedupKeyIndexName);

        Assert.True(index.IsUnique);
        Assert.Equal(nameof(OutboxMessage.DedupKey), Assert.Single(index.Properties).Name);

        // Abandonment must not release a key, so the filter turns only on PublishedAtUtc.
        Assert.Equal("\"DedupKey\" IS NOT NULL AND \"PublishedAtUtc\" IS NULL", index.GetFilter());
        Assert.DoesNotContain("ProcessingAbandonedAtUtc", index.GetFilter()!, StringComparison.Ordinal);
    }

    private static EfStorefrontPriceValidityScanRepository CreateScanRepository(PlatformDbContext dbContext)
    {
        return new EfStorefrontPriceValidityScanRepository(dbContext);
    }

    private static PlatformDbContext CreateDbContext()
    {
        return StorefrontPriceValidityScanFixtures.CreateDbContext();
    }

    private static Guid AddVariant(PlatformDbContext dbContext, Guid productId)
    {
        return StorefrontPriceValidityScanFixtures.AddVariant(dbContext, productId, NowUtc);
    }

    private static PriceList CreatePriceList(string code, DateTime? validFromUtc, DateTime? validToUtc)
    {
        return StorefrontPriceValidityScanFixtures.CreatePriceList(code, validFromUtc, validToUtc, NowUtc);
    }

    private static StorefrontProductProjection CreateProjection(Guid productId, string cultureCode, DateTime projectedAtUtc)
    {
        return StorefrontPriceValidityScanFixtures.CreateProjection(productId, cultureCode, projectedAtUtc);
    }
}
