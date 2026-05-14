using Platform.Application.Catalog.Attributes.Queries;
using Platform.Application.Catalog.Markets.Queries;
using Platform.Application.Catalog.Variants.Queries;
using Platform.Domain.Catalog.Attributes;
using Platform.Domain.Catalog.Markets;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Attributes;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Media;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Persistence;

namespace Platform.Tests;

public sealed class LookupApplicationServiceTests
{
    [Fact]
    public async Task MarketLookups_FilterByEnabledCurrency()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var germany = new Market(
            Guid.NewGuid(),
            "DE",
            "Germany",
            "EUR",
            "de-DE",
            "Net",
            now,
            now);
        store.Markets[germany.Id] = germany;

        var service = new MarketAdminApplicationService(
            new InMemoryMarketRepository(store),
            new InMemoryProductRepository(store),
            new InMemoryUnitOfWork());

        var results = await service.ListLookupsAsync(
            new ListMarketLookupsQuery(Search: null, Status: "Active", CurrencyCode: "SEK"),
            CancellationToken.None);

        var market = Assert.Single(results);
        Assert.Equal("SE", market.Code);
        Assert.Contains("SEK", market.CurrencyCodes);
    }

    [Fact]
    public async Task ProductAttributeEditorDefinitions_FilterByScopeAndSortBySortOrder()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var sizeAttribute = new ProductAttribute(
            Guid.NewGuid(),
            "SIZE",
            "Size",
            "Product",
            "Text",
            false,
            false,
            false,
            5,
            now,
            now,
            []);
        store.ProductAttributes[sizeAttribute.Id] = sizeAttribute;

        var service = new InMemoryProductAttributeAdminApplicationService(
            new InMemoryProductAttributeRepository(store),
            new InMemoryUnitOfWork());

        var results = await service.ListEditorDefinitionsAsync(
            new ListProductAttributeEditorDefinitionsQuery("Product", "Active"),
            CancellationToken.None);

        Assert.Equal(["SIZE", "POWER_SOURCE"], results.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task VariantLookups_IncludeProductContextForLabels()
    {
        var store = new InMemoryCatalogStore();
        var service = new InMemoryVariantAdminApplicationService(
            new InMemoryVariantRepository(store),
            new InMemoryProductRepository(store),
            new InMemoryMediaAssetRepository(store),
            new InMemoryProductStatusDefinitionRepository(store),
            new InMemoryUnitOfWork());

        var results = await service.ListLookupsAsync(
            new ListVariantLookupsQuery(Search: null, Status: "Active", ProductId: null),
            CancellationToken.None);

        var variant = Assert.Single(results);
        Assert.Equal("SKU-EXAMPLE-1-BLACK", variant.Sku);
        Assert.Equal("SKU-EXAMPLE-1", variant.ProductNumber);
        Assert.Equal("Example Drill", variant.ProductDefaultName);
    }
}
