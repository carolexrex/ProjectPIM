using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Application.Storefront;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

/// <summary>
/// Shared in-memory rig for the time-driven price-validity scheduler: it seeds priced products,
/// projects them at a chosen instant, and wires schedulers and refresh processors over one store.
/// </summary>
internal sealed class StorefrontPriceValidityTestContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public StorefrontPriceValidityTestContext()
    {
        Store = new InMemoryCatalogStore();
    }

    public InMemoryCatalogStore Store { get; }

    public Guid SeededProductId => StorefrontProjectionBuilderTests.ProductId;

    /// <summary>
    /// Points the seeded product's base price entry at a validity window, so a boundary passes
    /// without any catalog mutation.
    /// </summary>
    public void SetSeededProductEntryWindow(DateTime? validFromUtc, DateTime? validToUtc)
    {
        StorefrontProjectionBuilderTests.SetBaseEntryWindow(Store, validFromUtc, validToUtc);
    }

    /// <summary>
    /// Adds another product priced on the base list with an entry window that opens at
    /// <paramref name="boundaryUtc"/>.
    /// </summary>
    public Guid AddPricedProduct(int index, DateTime boundaryUtc)
    {
        var template = Store.Products[SeededProductId];
        var templateVariant = Store.Variants[StorefrontProjectionBuilderTests.VariantId];
        var createdAtUtc = boundaryUtc.AddDays(-1);

        var product = new Product(
            Guid.Parse($"50000000-0000-0000-0000-{index:D12}"),
            template.ProductType,
            $"SKU-EXAMPLE-{index}",
            $"example-drill-{index}",
            template.BrandId,
            template.ProductStatus,
            template.TaxCategoryCode,
            template.UnitOfMeasure,
            true,
            template.CategoryAssignments.Select(x => x.CategoryId).ToList(),
            [],
            null,
            null,
            null,
            null,
            createdAtUtc,
            createdAtUtc);
        product.UpsertTranslation("en-GB", $"Example Drill {index}", null, null, null, null);
        Store.Products[product.Id] = product;

        var variant = new Variant(
            Guid.Parse($"50000000-0000-0000-0001-{index:D12}"),
            product.Id,
            $"SKU-EXAMPLE-{index}-BLACK",
            null,
            null,
            null,
            templateVariant.ProductStatus,
            true,
            null,
            null,
            null,
            null,
            createdAtUtc,
            createdAtUtc,
            []);
        Store.Variants[variant.Id] = variant;

        var market = Store.Markets.Values.Single(x => string.Equals(x.Code, "SE", StringComparison.OrdinalIgnoreCase));
        market.UpsertProductAssignment(product.Id, "Active", market.RowVersion);

        var priceList = Store.PriceLists[StorefrontProjectionBuilderTests.BasePriceListId];
        priceList.UpsertEntry(null, "Variant", variant.Id, 1, 1299m, null, boundaryUtc, null, priceList.RowVersion);

        return product.Id;
    }

    public Task ProjectAsync(DateTime instantUtc, Guid? productId = null)
    {
        return CreateRefreshService(instantUtc).RefreshProductAsync(productId ?? SeededProductId, CancellationToken.None);
    }

    public StorefrontProjectionRefreshService CreateRefreshService(DateTime instantUtc)
    {
        return new StorefrontProjectionRefreshService(
            StorefrontProjectionBuilderTests.CreateBuilder(Store, instantUtc),
            new InMemoryStorefrontProductProjectionRepository(Store),
            new InMemoryProductRepository(Store),
            new InMemoryUnitOfWork());
    }

    public StorefrontPriceValidityRefreshService CreateScheduler(
        DateTime nowUtc,
        IStorefrontProjectionRefreshRequestPublisher? refreshRequestPublisher = null)
    {
        return new StorefrontPriceValidityRefreshService(
            new InMemoryStorefrontPriceValidityScanRepository(Store),
            refreshRequestPublisher ?? CreatePublisher(),
            new InMemoryUnitOfWork(),
            new FixedTimeProvider(nowUtc),
            NullLogger<StorefrontPriceValidityRefreshService>.Instance);
    }

    public StorefrontProjectionRefreshRequestPublisher CreatePublisher()
    {
        return new StorefrontProjectionRefreshRequestPublisher(
            new OutboxEventPublisher(new InMemoryOutboxMessageRepository(Store)));
    }

    public StorefrontProjectionOutboxProcessor CreateProcessor(
        DateTime instantUtc,
        IStorefrontProjectionRefreshService? refreshService = null)
    {
        return new StorefrontProjectionOutboxProcessor(
            new InMemoryOutboxMessageRepository(Store),
            refreshService ?? CreateRefreshService(instantUtc),
            new InMemoryVariantRepository(Store),
            new NoOpStorefrontProjectionChangeTracker(),
            new InMemoryUnitOfWork(),
            NullLogger<StorefrontProjectionOutboxProcessor>.Instance);
    }

    public IReadOnlyList<OutboxMessage> RefreshMessages()
    {
        return Store.OutboxMessages.Values
            .Where(x => string.Equals(x.EventType, WebhookEventTypes.StorefrontProjectionRefreshRequested, StringComparison.Ordinal))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public IReadOnlyList<OutboxMessage> OpenRefreshMessages()
    {
        return RefreshMessages().Where(x => x.IsOpen).ToList();
    }

    /// <summary>Messages still holding their dedup key, abandoned ones included.</summary>
    public IReadOnlyList<OutboxMessage> ReservedRefreshMessages()
    {
        return RefreshMessages().Where(x => x.ReservesDedupKey).ToList();
    }

    /// <summary>
    /// Puts the single scheduled request into the state the refresh processor leaves it in after it
    /// exhausts its retries.
    /// </summary>
    public OutboxMessage AbandonScheduledRefresh()
    {
        var message = Assert.Single(OpenRefreshMessages());
        message.MarkProcessingAbandoned("forced failure", message.RowVersion);
        return message;
    }

    /// <summary>Marks the single scheduled request published, as a successful refresh does.</summary>
    public OutboxMessage PublishScheduledRefresh()
    {
        var message = Assert.Single(OpenRefreshMessages());
        message.MarkPublished(message.RowVersion);
        return message;
    }

    public static StorefrontProjectionRefreshRequestedPayload ReadPayload(OutboxMessage message)
    {
        return JsonSerializer.Deserialize<StorefrontProjectionRefreshRequestedPayload>(message.PayloadJson, JsonOptions)!;
    }

    public static Guid ReadSingleProductId(OutboxMessage message)
    {
        return ReadPayload(message).ProductIds.Single();
    }

    public decimal? ProjectedPrice(Guid productId)
    {
        return Store.StorefrontProductProjections.Values
            .Where(x => x.ProductId == productId)
            .Select(x => x.PriceAmount)
            .FirstOrDefault();
    }
}
