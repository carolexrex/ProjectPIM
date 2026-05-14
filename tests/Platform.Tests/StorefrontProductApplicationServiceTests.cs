using Platform.Application.Storefront;
using Platform.Domain.Catalog.Markets;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Attributes;
using Platform.Infrastructure.Catalog.Brands;
using Platform.Infrastructure.Catalog.Categories;
using Platform.Infrastructure.Catalog.Channels;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Media;
using Platform.Infrastructure.Catalog.Pricing;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

public sealed class StorefrontProductApplicationServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsPagedSummariesWithResolvedCommerceData()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.ListAsync(
            new GetStorefrontProductsQuery(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "tools",
                "ACME",
                "example",
                "name",
                1,
                24),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Products);
        Assert.Equal(1, result.Products!.Total);
        Assert.Equal("name", result.Products.AppliedFilters.Sort);
        Assert.Contains("name", result.Products.Facets.SortOptions);
        Assert.Contains(result.Products.Facets.Categories, x => x.Code == "TOOLS" && x.Count == 1);
        Assert.Contains(result.Products.Facets.Brands, x => x.Code == "ACME" && x.Count == 1);

        var product = Assert.Single(result.Products.Items);
        Assert.Equal("SKU-EXAMPLE-1", product.ProductNumber);
        Assert.Equal("Example Drill", product.Name);
        Assert.Equal("ACME", product.Brand!.Code);
        Assert.Equal("https://images.example.com/drill-hero.jpg", product.PrimaryImageUrl);
        Assert.NotNull(product.Price);
        Assert.Equal("SEK", product.Price!.CurrencyCode);
        Assert.Equal(1499m, product.Price.Amount);
        Assert.Equal("SE_BASE_GROSS", product.Price.PriceListCode);
        Assert.Equal("InStock", product.Availability.Status);
        Assert.Equal(23m, product.Availability.AvailableQuantity);
        Assert.True(product.Buyability.IsVisible);
        Assert.True(product.Buyability.IsBuyable);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsDetailsWithVariantDiagnostics()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.GetBySlugAsync(
            new GetStorefrontProductBySlugQuery(
                "example-drill",
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Product);

        var product = result.Product!;
        Assert.Equal("Example Drill", product.Name);
        Assert.Equal("Hardware", product.ProductType);
        Assert.Equal("Acme Tools", product.Brand!.Name);
        Assert.Contains(product.Categories, x => x.Code == "DRILLS");
        Assert.Single(product.Media);
        Assert.Single(product.Attributes);
        Assert.Single(product.Variants);
        Assert.Equal("SE_BASE_GROSS", product.Price!.PriceListCode);
        Assert.Equal("InStock", product.Availability.Status);

        var variant = product.Variants[0];
        Assert.Equal("SKU-EXAMPLE-1-BLACK", variant.Sku);
        Assert.True(variant.IsDefaultVariant);
        Assert.Equal("InStock", variant.Availability.Status);
        Assert.True(variant.Buyability.IsBuyable);
        Assert.Single(variant.Attributes);
        Assert.Equal("COLOR", variant.Attributes[0].AttributeCode);
    }

    [Fact]
    public async Task GetByProductNumberAsync_ReturnsDetailsForStableCommerceIdentifier()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.GetByProductNumberAsync(
            new GetStorefrontProductByProductNumberQuery(
                "SKU-EXAMPLE-1",
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Product);
        Assert.Equal("example-drill", result.Product!.Slug);
        Assert.Equal("SKU-EXAMPLE-1", result.Product.ProductNumber);
    }

    [Fact]
    public async Task ListAsync_ReturnsValidationFailureForUnsupportedSort()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.ListAsync(
            new GetStorefrontProductsQuery(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                null,
                null,
                null,
                "price",
                1,
                24),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, result.Status);
        Assert.Contains(nameof(GetStorefrontProductsQuery.Sort), result.Errors.Keys);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNotFoundWhenProductIsNotVisibleInRequestedMarket()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var noMarket = new Market(
            Guid.Parse("62000000-0000-0000-0000-000000000009"),
            "NO",
            "Norway",
            "NOK",
            "nb-NO",
            "Gross",
            now,
            now);
        store.Markets[noMarket.Id] = noMarket;

        var channel = store.Channels[Guid.Parse("63000000-0000-0000-0000-000000000001")];
        channel.UpsertMarketAssignment(noMarket.Id, channel.RowVersion);

        var service = CreateService(store);

        var result = await service.GetBySlugAsync(
            new GetStorefrontProductBySlugQuery(
                "example-drill",
                "WEB-SE",
                "NO",
                "nb-NO",
                "NOK",
                null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.NotFound, result.Status);
        Assert.Equal("Product", result.ResourceName);
    }

    private static StorefrontProductApplicationService CreateService(InMemoryCatalogStore store)
    {
        var contextService = new StorefrontContextApplicationService(
            new InMemoryChannelRepository(store),
            new InMemoryMarketRepository(store));
        var projectionRepository = new InMemoryStorefrontProductProjectionRepository(store);
        var refreshService = new StorefrontProjectionRefreshService(
            new StorefrontProjectionBuilder(
                new InMemoryBrandRepository(store),
                new InMemoryCategoryRepository(store),
                new InMemoryInventoryBalanceRepository(store),
                new InMemoryInventoryLocationRepository(store),
                new InMemoryMarketRepository(store),
                new InMemoryMediaAssetRepository(store),
                new InMemoryPriceListRepository(store),
                new InMemoryProductAttributeRepository(store),
                new InMemoryProductRepository(store),
                new InMemoryVariantRepository(store)),
            projectionRepository,
            new InMemoryProductRepository(store),
            new InMemoryUnitOfWork());

        return new StorefrontProductApplicationService(
            new InMemoryBrandRepository(store),
            new InMemoryCategoryRepository(store),
            contextService,
            projectionRepository,
            refreshService);
    }
}
