using Platform.Application.Storefront;
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

    private static StorefrontProjectionBuilder CreateBuilder(InMemoryCatalogStore store)
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
            new InMemoryVariantRepository(store));
    }
}
