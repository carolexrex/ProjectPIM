using System.Text.Json;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Domain.Catalog.Pricing;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Attributes;
using Platform.Infrastructure.Catalog.Brands;
using Platform.Infrastructure.Catalog.Categories;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Media;
using Platform.Infrastructure.Catalog.Pricing;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

public sealed class StorefrontProjectionBuilderTests
{
    [Fact]
    public async Task BuildForProductAsync_CreatesProjectedStorefrontSnapshot()
    {
        var store = new InMemoryCatalogStore();
        var builder = CreateBuilder(store);

        var projections = await builder.BuildForProductAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None);

        var projection = Assert.Single(projections);
        Assert.Equal("SE", projection.MarketCode);
        Assert.Equal("sv-SE", projection.CultureCode);
        Assert.Equal("SEK", projection.CurrencyCode);
        Assert.Equal("SKU-EXAMPLE-1", projection.ProductNumber);
        Assert.Equal("example-drill", projection.Slug);
        Assert.True(projection.IsVisible);
        Assert.True(projection.IsBuyable);
        Assert.Equal(1499m, projection.PriceAmount);
        Assert.Equal("SE_BASE_GROSS", projection.PriceListCode);
        Assert.Equal("InStock", projection.AvailabilityStatus);
        Assert.Equal(23m, projection.AvailableQuantity);
        Assert.Contains("drills", projection.CategorySlugsJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SKU-EXAMPLE-1-BLACK", projection.VariantsJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshProductAsync_ReplacesProjectionRowsForProduct()
    {
        var store = new InMemoryCatalogStore();
        var builder = CreateBuilder(store);
        var repository = new InMemoryStorefrontProductProjectionRepository(store);
        var refreshService = new StorefrontProjectionRefreshService(
            builder,
            repository,
            new InMemoryProductRepository(store),
            new InMemoryUnitOfWork());

        await refreshService.RefreshProductAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None);

        var projections = await repository.ListByProductIdAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None);

        var projection = Assert.Single(projections);
        Assert.Equal("SE", projection.MarketCode);
        Assert.Equal("SKU-EXAMPLE-1", projection.ProductNumber);
    }

    [Fact]
    public async Task BuildForProductAsync_IgnoresPriceEntryThatIsNotValidYet()
    {
        var store = new InMemoryCatalogStore();
        var now = ProjectionInstantUtc;
        SetBaseEntryWindow(store, now.AddHours(1), null);

        var projection = Assert.Single(await CreateBuilder(store, now).BuildForProductAsync(ProductId, CancellationToken.None));

        Assert.Null(projection.PriceAmount);
        Assert.Null(projection.PriceListCode);
        Assert.False(projection.IsBuyable);
        Assert.Contains("MissingPrice", ReadBuyabilityReasons(projection));
        Assert.False(ReadVariants(projection).Single().Buyability.IsBuyable);
        Assert.Contains("MissingPrice", ReadVariants(projection).Single().Buyability.Reasons);
    }

    [Fact]
    public async Task BuildForProductAsync_IgnoresExpiredPriceEntry()
    {
        var store = new InMemoryCatalogStore();
        var now = ProjectionInstantUtc;
        SetBaseEntryWindow(store, now.AddHours(-2), now.AddHours(-1));

        var projection = Assert.Single(await CreateBuilder(store, now).BuildForProductAsync(ProductId, CancellationToken.None));

        Assert.Null(projection.PriceAmount);
        Assert.False(projection.IsBuyable);
        Assert.Contains("MissingPrice", ReadBuyabilityReasons(projection));
    }

    [Fact]
    public async Task BuildForProductAsync_TreatsValidityBoundariesAsInclusive()
    {
        var store = new InMemoryCatalogStore();
        var now = ProjectionInstantUtc;
        SetBaseEntryWindow(store, now, now);

        var projection = Assert.Single(await CreateBuilder(store, now).BuildForProductAsync(ProductId, CancellationToken.None));

        Assert.Equal(1499m, projection.PriceAmount);
        Assert.Equal("SE_BASE_GROSS", projection.PriceListCode);
        Assert.True(projection.IsBuyable);
    }

    [Fact]
    public async Task BuildForProductAsync_FallsBackToAnotherValidEntryInTheSamePriceList()
    {
        var store = new InMemoryCatalogStore();
        var now = ProjectionInstantUtc;
        SetBaseEntryWindow(store, now.AddHours(-2), now.AddHours(-1));

        var priceList = store.PriceLists[BasePriceListId];
        priceList.UpsertEntry(Guid.NewGuid(), "Variant", VariantId, 1, 1399m, 1599m, now.AddHours(-1), null, priceList.RowVersion);

        var projection = Assert.Single(await CreateBuilder(store, now).BuildForProductAsync(ProductId, CancellationToken.None));

        Assert.Equal(1399m, projection.PriceAmount);
        Assert.Equal(1599m, projection.CompareAtAmount);
        Assert.Equal("SE_BASE_GROSS", projection.PriceListCode);
    }

    [Fact]
    public async Task BuildForProductAsync_FallsBackToLowerPriorityPriceListWhenEntryExpired()
    {
        var store = new InMemoryCatalogStore();
        var now = ProjectionInstantUtc;
        SetBaseEntryWindow(store, now.AddHours(-2), now.AddHours(-1));
        AddFallbackPriceList(store, now, 1699m, vatIncluded: false);

        var projection = Assert.Single(await CreateBuilder(store, now).BuildForProductAsync(ProductId, CancellationToken.None));

        Assert.Equal(1699m, projection.PriceAmount);
        Assert.Equal("SE_FALLBACK_NET", projection.PriceListCode);
        Assert.False(projection.VatIncluded);
        Assert.True(projection.IsBuyable);
    }

    [Fact]
    public async Task BuildForProductAsync_KeepsQuantityBreakEntriesOutOfTheStorefrontUnitPrice()
    {
        var store = new InMemoryCatalogStore();
        var now = ProjectionInstantUtc;

        var priceList = store.PriceLists[BasePriceListId];
        priceList.UpsertEntry(null, "Variant", VariantId, 10, 999m, null, null, null, priceList.RowVersion);

        var projection = Assert.Single(await CreateBuilder(store, now).BuildForProductAsync(ProductId, CancellationToken.None));

        Assert.Equal(1499m, projection.PriceAmount);
    }

    internal static readonly DateTime ProjectionInstantUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
    internal static readonly Guid ProductId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    internal static readonly Guid VariantId = Guid.Parse("50000000-0000-0000-0000-000000000011");
    internal static readonly Guid BasePriceListId = Guid.Parse("64000000-0000-0000-0000-000000000001");

    internal static void SetBaseEntryWindow(InMemoryCatalogStore store, DateTime? validFromUtc, DateTime? validToUtc)
    {
        var priceList = store.PriceLists[BasePriceListId];
        priceList.UpsertEntry(null, "Variant", VariantId, 1, 1499m, 1699m, validFromUtc, validToUtc, priceList.RowVersion);
    }

    private static void AddFallbackPriceList(InMemoryCatalogStore store, DateTime now, decimal amount, bool vatIncluded)
    {
        var marketId = store.Markets.Values.Single(x => string.Equals(x.Code, "SE", StringComparison.OrdinalIgnoreCase)).Id;
        var fallback = new PriceList(
            Guid.NewGuid(),
            "SE_FALLBACK_NET",
            "SE Fallback",
            "SEK",
            vatIncluded,
            null,
            null,
            now.AddDays(-1),
            now.AddDays(-1));

        fallback.UpsertMarketAssignment(marketId, 1, false, fallback.RowVersion);
        fallback.UpsertEntry(null, "Variant", VariantId, 1, amount, null, now.AddHours(-1), now.AddHours(1), fallback.RowVersion);
        store.PriceLists[fallback.Id] = fallback;
    }

    private static IReadOnlyList<string> ReadBuyabilityReasons(StorefrontProductProjection projection)
    {
        return JsonSerializer.Deserialize<List<string>>(projection.BuyabilityReasonsJson, JsonOptions) ?? [];
    }

    private static IReadOnlyList<StorefrontProductVariantDto> ReadVariants(StorefrontProductProjection projection)
    {
        return JsonSerializer.Deserialize<List<StorefrontProductVariantDto>>(projection.VariantsJson, JsonOptions) ?? [];
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static StorefrontProjectionBuilder CreateBuilder(InMemoryCatalogStore store, DateTime? instantUtc = null)
    {
        return new StorefrontProjectionBuilder(
            new InMemoryBrandRepository(store),
            new InMemoryCategoryRepository(store),
            new InMemoryInventoryBalanceRepository(store),
            new InMemoryInventoryLocationRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryMediaAssetRepository(store),
            new InMemoryPriceListRepository(store),
            new InMemoryProductAttributeRepository(store),
            new InMemoryProductRepository(store),
            new InMemoryVariantRepository(store),
            instantUtc.HasValue ? new FixedTimeProvider(instantUtc.Value) : TimeProvider.System);
    }
}
