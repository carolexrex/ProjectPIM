using Microsoft.Extensions.Logging.Abstractions;
using Platform.Application.Catalog.Brands.Commands;
using Platform.Application.Catalog.Categories.Commands;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Catalog.Markets.Commands;
using Platform.Application.Catalog.Pricing.Commands;
using Platform.Application.Storefront;
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

public sealed class StorefrontProjectionOutboxProcessorTests
{
    [Fact]
    public async Task ExecutePendingAsync_RefreshesProjectionForRequestedVariant()
    {
        var store = new InMemoryCatalogStore();
        var projectionRepository = new InMemoryStorefrontProductProjectionRepository(store);
        var refreshService = CreateRefreshService(store, projectionRepository);
        await refreshService.RefreshProductAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None);

        var originalProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None));
        Assert.Equal(1499m, originalProjection.PriceAmount);

        var priceList = store.PriceLists[Guid.Parse("64000000-0000-0000-0000-000000000001")];
        var variantId = Guid.Parse("50000000-0000-0000-0000-000000000011");
        var priceListService = CreatePriceListService(store);
        await priceListService.UpsertEntryAsync(
            new UpsertPriceListEntryCommand(
                priceList.Id,
                null,
                "Variant",
                variantId,
                1,
                1299m,
                1499m,
                null,
                null,
                priceList.RowVersion),
            CancellationToken.None);

        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);

        var processor = new StorefrontProjectionOutboxProcessor(
            new InMemoryOutboxMessageRepository(store),
            refreshService,
            new InMemoryVariantRepository(store),
            new InMemoryUnitOfWork(),
            NullLogger<StorefrontProjectionOutboxProcessor>.Instance);

        var processed = await processor.ExecutePendingAsync(1, CancellationToken.None);

        Assert.Equal(1, processed);
        var refreshMessage = Assert.Single(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);
        Assert.True(refreshMessage.IsPublished);
        var refreshedProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None));
        Assert.Equal(1299m, refreshedProjection.PriceAmount);
    }

    [Fact]
    public async Task ExecutePendingAsync_RefreshesProjectionForBrandUpdateFanOut()
    {
        var store = new InMemoryCatalogStore();
        var projectionRepository = new InMemoryStorefrontProductProjectionRepository(store);
        var refreshService = CreateRefreshService(store, projectionRepository);
        var productId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var brandId = Guid.Parse("61000000-0000-0000-0000-000000000001");
        await refreshService.RefreshProductAsync(productId, CancellationToken.None);

        var originalProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
        Assert.Equal("Acme Tools", originalProjection.BrandName);

        var brand = store.Brands[brandId];
        var brandService = CreateBrandService(store);
        await brandService.UpsertTranslationAsync(
            new UpsertBrandTranslationCommand(
                brand.Id,
                "sv-SE",
                "Acme Verktyg",
                "acme-verktyg",
                "Svensk beskrivning."),
            CancellationToken.None);

        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);

        var processor = new StorefrontProjectionOutboxProcessor(
            new InMemoryOutboxMessageRepository(store),
            refreshService,
            new InMemoryVariantRepository(store),
            new InMemoryUnitOfWork(),
            NullLogger<StorefrontProjectionOutboxProcessor>.Instance);

        var processed = await processor.ExecutePendingAsync(10, CancellationToken.None);

        Assert.Equal(1, processed);
        var refreshedProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
        Assert.Equal("Acme Verktyg", refreshedProjection.BrandName);
    }

    [Fact]
    public async Task ExecutePendingAsync_RefreshesProjectionForCategorySubtreeFanOut()
    {
        var store = new InMemoryCatalogStore();
        var projectionRepository = new InMemoryStorefrontProductProjectionRepository(store);
        var refreshService = CreateRefreshService(store, projectionRepository);
        var productId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var parentCategoryId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        await refreshService.RefreshProductAsync(productId, CancellationToken.None);

        var originalProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
        Assert.Contains("tools", originalProjection.CategoryFilterSlugsJson);

        var categoryService = CreateCategoryService(store);
        await categoryService.UpsertTranslationAsync(
            new UpsertCategoryTranslationCommand(
                parentCategoryId,
                "sv-SE",
                "Verktyg",
                "verktyg",
                "Svensk beskrivning."),
            CancellationToken.None);

        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);

        var processor = new StorefrontProjectionOutboxProcessor(
            new InMemoryOutboxMessageRepository(store),
            refreshService,
            new InMemoryVariantRepository(store),
            new InMemoryUnitOfWork(),
            NullLogger<StorefrontProjectionOutboxProcessor>.Instance);

        var processed = await processor.ExecutePendingAsync(10, CancellationToken.None);

        Assert.Equal(1, processed);
        var refreshedProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
        Assert.Contains("verktyg", refreshedProjection.CategoryFilterSlugsJson);
    }

    [Fact]
    public async Task ExecutePendingAsync_RefreshesProjectionForMarketProductAssignmentFanOut()
    {
        var store = new InMemoryCatalogStore();
        var projectionRepository = new InMemoryStorefrontProductProjectionRepository(store);
        var refreshService = CreateRefreshService(store, projectionRepository);
        var productId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var market = store.Markets[Guid.Parse("62000000-0000-0000-0000-000000000001")];
        await refreshService.RefreshProductAsync(productId, CancellationToken.None);

        var originalProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
        Assert.True(originalProjection.IsVisible);

        var marketService = CreateMarketService(store);
        await marketService.UpsertProductAssignmentAsync(
            new UpsertMarketProductAssignmentCommand(market.Id, productId, "Inactive", market.RowVersion),
            CancellationToken.None);

        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);

        var processor = new StorefrontProjectionOutboxProcessor(
            new InMemoryOutboxMessageRepository(store),
            refreshService,
            new InMemoryVariantRepository(store),
            new InMemoryUnitOfWork(),
            NullLogger<StorefrontProjectionOutboxProcessor>.Instance);

        var processed = await processor.ExecutePendingAsync(10, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Empty(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
    }

    [Fact]
    public async Task ExecutePendingAsync_RefreshesProjectionForInventoryLocationMarketAssignmentFanOut()
    {
        var store = new InMemoryCatalogStore();
        var projectionRepository = new InMemoryStorefrontProductProjectionRepository(store);
        var refreshService = CreateRefreshService(store, projectionRepository);
        var productId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var location = store.InventoryLocations[Guid.Parse("65000000-0000-0000-0000-000000000001")];
        var marketId = Guid.Parse("62000000-0000-0000-0000-000000000001");
        await refreshService.RefreshProductAsync(productId, CancellationToken.None);

        var originalProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
        Assert.Equal("InStock", originalProjection.AvailabilityStatus);
        Assert.True(originalProjection.IsBuyable);

        var inventoryService = CreateInventoryService(store);
        await inventoryService.RemoveLocationMarketAssignmentAsync(
            new RemoveInventoryLocationMarketAssignmentCommand(location.Id, marketId, location.RowVersion),
            CancellationToken.None);

        Assert.Contains(store.OutboxMessages.Values, x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested);

        var processor = new StorefrontProjectionOutboxProcessor(
            new InMemoryOutboxMessageRepository(store),
            refreshService,
            new InMemoryVariantRepository(store),
            new InMemoryUnitOfWork(),
            NullLogger<StorefrontProjectionOutboxProcessor>.Instance);

        var processed = await processor.ExecutePendingAsync(10, CancellationToken.None);

        Assert.Equal(1, processed);
        var refreshedProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(productId, CancellationToken.None));
        Assert.Equal("Unavailable", refreshedProjection.AvailabilityStatus);
        Assert.False(refreshedProjection.IsBuyable);
    }

    [Fact]
    public async Task WebhookOutboxExecution_IgnoresInternalRefreshRequests()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Product updates",
            "https://example.test/hooks/products",
            "secret",
            [WebhookEventTypes.ProductUpdated],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var refreshMessage = new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            "StorefrontProductProjection",
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            "{\"productIds\":[\"50000000-0000-0000-0000-000000000001\"],\"variantIds\":[],\"reason\":\"Test\",\"requestedAtUtc\":\"2026-05-16T00:00:00Z\"}",
            now);
        var productMessage = new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "Product",
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            "{\"event\":\"product.updated\"}",
            now.AddSeconds(1));
        store.OutboxMessages[refreshMessage.Id] = refreshMessage;
        store.OutboxMessages[productMessage.Id] = productMessage;

        var service = new WebhookOutboxExecutionService(
            new InMemoryOutboxMessageRepository(store),
            new InMemoryWebhookSubscriptionRepository(store),
            new InMemoryWebhookDeliveryRepository(store),
            new InMemoryUnitOfWork(),
            NullLogger<WebhookOutboxExecutionService>.Instance);

        var published = await service.ExecutePendingAsync(1, CancellationToken.None);

        Assert.Equal(1, published);
        Assert.False(refreshMessage.IsPublished);
        Assert.True(productMessage.IsPublished);
        var delivery = Assert.Single(store.WebhookDeliveries.Values);
        Assert.Equal(WebhookEventTypes.ProductUpdated, delivery.EventType);
    }

    private static StorefrontProjectionRefreshService CreateRefreshService(
        InMemoryCatalogStore store,
        IStorefrontProductProjectionRepository projectionRepository)
    {
        return new StorefrontProjectionRefreshService(
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

    private static InMemoryCategoryAdminApplicationService CreateCategoryService(InMemoryCatalogStore store)
    {
        return new InMemoryCategoryAdminApplicationService(
            new InMemoryCategoryRepository(store),
            new InMemoryProductRepository(store),
            new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store))),
            new InMemoryUnitOfWork());
    }

    private static MarketAdminApplicationService CreateMarketService(InMemoryCatalogStore store)
    {
        return new MarketAdminApplicationService(
            new InMemoryMarketRepository(store),
            new InMemoryProductRepository(store),
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
}
