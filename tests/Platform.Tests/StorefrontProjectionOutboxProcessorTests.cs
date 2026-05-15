using Microsoft.Extensions.Logging.Abstractions;
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
            NullLogger<StorefrontProjectionOutboxProcessor>.Instance);

        var processed = await processor.ExecutePendingAsync(10, CancellationToken.None);

        Assert.Equal(1, processed);
        var refreshedProjection = Assert.Single(await projectionRepository.ListByProductIdAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None));
        Assert.Equal(1299m, refreshedProjection.PriceAmount);
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
}
