using System.Text.Json;
using Platform.Application.Catalog.Brands.Commands;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Catalog.Pricing.Commands;
using Platform.Application.Catalog.Products.Commands;
using Platform.Contracts.Integrations;
using Platform.Domain.Integrations;
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
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

public sealed class CatalogMutationOutboxTests
{
    [Fact]
    public async Task BrandCreate_EnqueuesBrandCreatedEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateBrandService(store);
        var logoMediaAssetId = Guid.Parse("74000000-0000-0000-0000-000000000001");

        var created = await service.CreateAsync(
            new CreateBrandCommand("BOSCH", "https://bosch.example", logoMediaAssetId, 20),
            CancellationToken.None);

        var message = Assert.Single(store.OutboxMessages.Values);
        Assert.Equal(WebhookEventTypes.BrandCreated, message.EventType);
        Assert.Equal("Brand", message.AggregateType);
        Assert.Equal(created.Id, message.AggregateId);

        var payload = Deserialize<BrandWebhookEventDto>(message.PayloadJson);
        Assert.Equal("Created", payload.ChangeType);
        Assert.Equal("BOSCH", payload.Brand.Code);
        Assert.Equal("https://bosch.example", payload.Brand.WebsiteUrl);
    }

    [Fact]
    public async Task BrandTranslationUpsert_EnqueuesBrandUpdatedEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateBrandService(store);
        var brandId = Guid.Parse("61000000-0000-0000-0000-000000000001");

        var translation = await service.UpsertTranslationAsync(
            new UpsertBrandTranslationCommand(brandId, "sv-SE", "Acme Verktyg", "acme-verktyg", "Svensk beskrivning."),
            CancellationToken.None);

        Assert.NotNull(translation);

        var message = Assert.Single(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.BrandUpdated);
        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);
        Assert.Equal(WebhookEventTypes.BrandUpdated, message.EventType);

        var payload = Deserialize<BrandWebhookEventDto>(message.PayloadJson);
        Assert.Equal("TranslationUpserted", payload.ChangeType);
        Assert.Contains(payload.Brand.Translations, x => x.CultureCode == "sv-SE" && x.Name == "Acme Verktyg");
    }

    [Fact]
    public async Task ProductCreate_EnqueuesProductCreatedEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateProductService(store);
        var brandId = Guid.Parse("61000000-0000-0000-0000-000000000001");
        var readyStatusId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var drillsCategoryId = Guid.Parse("60000000-0000-0000-0000-000000000002");
        var powerSourceAttributeId = Guid.Parse("71000000-0000-0000-0000-000000000002");
        var cordlessOptionId = Guid.Parse("72000000-0000-0000-0000-000000000012");

        var created = await service.CreateAsync(
            new CreateProductCommand(
                "Hardware",
                "SKU-NEW-OUTBOX-1",
                "new-outbox-drill",
                brandId,
                readyStatusId,
                "STANDARD",
                "pcs",
                false,
                [drillsCategoryId],
                [new CreateProductAttributeValueCommand(powerSourceAttributeId, cordlessOptionId, null)],
                1.2m,
                20m,
                8m,
                10m),
            CancellationToken.None);

        var message = Assert.Single(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.ProductCreated);
        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);
        Assert.Equal(WebhookEventTypes.ProductCreated, message.EventType);
        Assert.Equal("Product", message.AggregateType);
        Assert.Equal(created.Id, message.AggregateId);

        var payload = Deserialize<ProductWebhookEventDto>(message.PayloadJson);
        Assert.Equal("Created", payload.ChangeType);
        Assert.Equal("SKU-NEW-OUTBOX-1", payload.Product.ProductNumber);
        Assert.Equal("new-outbox-drill", payload.Product.Slug);
    }

    [Fact]
    public async Task ProductTranslationUpsert_EnqueuesProductUpdatedEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateProductService(store);
        var productId = Guid.Parse("50000000-0000-0000-0000-000000000001");

        var translation = await service.UpsertTranslationAsync(
            new UpsertProductTranslationCommand(
                productId,
                "sv-SE",
                "Exempelborr",
                "Kort",
                "Lang",
                "SEO",
                "SEO desc"),
            CancellationToken.None);

        Assert.NotNull(translation);

        var message = Assert.Single(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.ProductUpdated);
        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);
        Assert.Equal(WebhookEventTypes.ProductUpdated, message.EventType);

        var payload = Deserialize<ProductWebhookEventDto>(message.PayloadJson);
        Assert.Equal("TranslationUpserted", payload.ChangeType);
        Assert.Contains(payload.Product.Translations, x => x.CultureCode == "sv-SE" && x.Name == "Exempelborr");
    }

    [Fact]
    public async Task PriceListCreate_EnqueuesPriceListCreatedEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = CreatePriceListService(store);

        var created = await service.CreateAsync(
            new CreatePriceListCommand("SE_CAMPAIGN", "SE Campaign", "SEK", true, null, null),
            CancellationToken.None);

        var message = Assert.Single(store.OutboxMessages.Values);
        Assert.Equal(WebhookEventTypes.PriceListCreated, message.EventType);
        Assert.Equal("PriceList", message.AggregateType);
        Assert.Equal(created.Id, message.AggregateId);

        var payload = Deserialize<PriceListWebhookEventDto>(message.PayloadJson);
        Assert.Equal("Created", payload.ChangeType);
        Assert.Equal("SE_CAMPAIGN", payload.PriceList.Code);
        Assert.Empty(payload.PriceList.Entries);
    }

    [Fact]
    public async Task PriceListEntryUpsert_EnqueuesPriceListUpdatedEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = CreatePriceListService(store);
        var priceList = store.PriceLists[Guid.Parse("64000000-0000-0000-0000-000000000001")];
        var variantId = Guid.Parse("50000000-0000-0000-0000-000000000011");

        var updated = await service.UpsertEntryAsync(
            new UpsertPriceListEntryCommand(
                priceList.Id,
                null,
                "Variant",
                variantId,
                2,
                1399m,
                1499m,
                null,
                null,
                priceList.RowVersion),
            CancellationToken.None);

        Assert.NotNull(updated);

        var message = Assert.Single(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.PriceListUpdated);
        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);
        Assert.Equal(WebhookEventTypes.PriceListUpdated, message.EventType);

        var payload = Deserialize<PriceListWebhookEventDto>(message.PayloadJson);
        Assert.Equal("EntryUpserted", payload.ChangeType);
        Assert.Contains(payload.PriceList.Entries, x => x.MinQuantity == 2 && x.Amount == 1399m);
    }

    [Fact]
    public async Task InventoryBalanceUpsert_EnqueuesStorefrontProjectionRefreshRequest()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateInventoryService(store);
        var balance = store.InventoryBalances[Guid.Parse("66000000-0000-0000-0000-000000000001")];

        await service.UpsertBalanceAsync(
            new UpsertInventoryBalanceCommand(
                balance.InventoryLocationId,
                balance.VariantId,
                30m,
                1m,
                0m,
                false,
                balance.RowVersion),
            CancellationToken.None);

        var message = Assert.Single(store.OutboxMessages.Values);
        Assert.Equal(WebhookEventTypes.StorefrontProjectionRefreshRequested, message.EventType);
    }

    private static BrandAdminApplicationService CreateBrandService(InMemoryCatalogStore store)
    {
        return new BrandAdminApplicationService(
            new InMemoryBrandRepository(store),
            new InMemoryMediaAssetRepository(store),
            new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store)),
            new InMemoryProductRepository(store),
            new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store))),
            new InMemoryUnitOfWork());
    }

    private static InMemoryProductAdminApplicationService CreateProductService(InMemoryCatalogStore store)
    {
        return new InMemoryProductAdminApplicationService(
            new InMemoryProductRepository(store),
            new InMemoryBrandRepository(store),
            new InMemoryCategoryRepository(store),
            new InMemoryProductAttributeRepository(store),
            new InMemoryMediaAssetRepository(store),
            new InMemoryProductStatusDefinitionRepository(store),
            new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store)),
            new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store))),
            new InMemoryUnitOfWork());
    }

    private static PriceListAdminApplicationService CreatePriceListService(InMemoryCatalogStore store)
    {
        return new PriceListAdminApplicationService(
            new InMemoryPriceListRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryVariantRepository(store),
            new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store)),
            new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store))),
            new InMemoryUnitOfWork());
    }

    private static InventoryAdminApplicationService CreateInventoryService(InMemoryCatalogStore store)
    {
        return new InventoryAdminApplicationService(
            new InMemoryInventoryLocationRepository(store),
            new InMemoryInventoryBalanceRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryVariantRepository(store),
            new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store))),
            new InMemoryUnitOfWork());
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Unable to deserialize {typeof(T).Name}.");
    }
}
