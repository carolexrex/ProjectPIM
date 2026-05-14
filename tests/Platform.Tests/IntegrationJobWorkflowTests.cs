using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Application.Abstractions.Security;
using Platform.Application.Integrations.Commands;
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

public sealed class IntegrationJobWorkflowTests
{
    [Fact]
    public async Task CreateBrandExportJob_PersistsPendingJobForCurrentActor()
    {
        var store = new InMemoryCatalogStore();
        var service = new IntegrationJobAdminApplicationService(
            new InMemoryIntegrationJobRepository(store),
            new StubCurrentActorAccessor("catalog-admin"),
            new InMemoryUnitOfWork());

        var created = await service.CreateBrandExportAsync(
            new CreateBrandExportJobCommand("acme", "Active"),
            CancellationToken.None);

        Assert.Equal(IntegrationJobTypes.BrandExport, created.Type);
        Assert.Equal(IntegrationJobDirections.Export, created.Direction);
        Assert.Equal(IntegrationJobStatuses.Pending, created.Status);
        Assert.Equal("catalog-admin", created.RequestedBy);

        var stored = Assert.Single(store.IntegrationJobs.Values);
        Assert.Equal(created.Id, stored.Id);
        Assert.Contains("\"search\":\"acme\"", stored.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecutePendingAsync_CompletesBrandExportJobAndStoresResult()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var job = new IntegrationJob(
            Guid.NewGuid(),
            IntegrationJobTypes.BrandExport,
            IntegrationJobDirections.Export,
            "catalog-admin",
            JsonSerializer.Serialize(new BrandExportJobPayload(null, "Active"), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            now);
        store.IntegrationJobs[job.Id] = job;

        var service = CreateExecutionService(store);
        var executed = await service.ExecutePendingAsync(5, CancellationToken.None);

        Assert.Equal(1, executed);

        var completed = store.IntegrationJobs[job.Id];
        Assert.Equal(IntegrationJobStatuses.Completed, completed.Status);
        Assert.Equal(1, completed.AttemptCount);
        Assert.NotNull(completed.CompletedAtUtc);
        Assert.Equal("Exported 1 brands.", completed.ResultSummary);

        var result = JsonSerializer.Deserialize<BrandExportJobResult>(completed.ResultPayloadJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalCount);
        Assert.Equal("ACME", Assert.Single(result.Items).Code);
    }

    [Fact]
    public async Task CreateBrandImportJob_PersistsPendingJobForCurrentActor()
    {
        var store = new InMemoryCatalogStore();
        var service = new IntegrationJobAdminApplicationService(
            new InMemoryIntegrationJobRepository(store),
            new StubCurrentActorAccessor("catalog-admin"),
            new InMemoryUnitOfWork());

        var created = await service.CreateBrandImportAsync(
            new CreateBrandImportJobCommand(
                [
                    new BrandImportJobItemInput(
                        "NEW-BRAND",
                        "https://example.com/new",
                        null,
                        20,
                        [
                            new BrandImportJobTranslationInput(
                                "en-GB",
                                "New Brand",
                                "new-brand",
                                "Imported brand")
                        ])
                ]),
            CancellationToken.None);

        Assert.Equal(IntegrationJobTypes.BrandImport, created.Type);
        Assert.Equal(IntegrationJobDirections.Import, created.Direction);
        Assert.Equal(IntegrationJobStatuses.Pending, created.Status);
        Assert.Equal("catalog-admin", created.RequestedBy);

        var stored = Assert.Single(store.IntegrationJobs.Values, x => x.Type == IntegrationJobTypes.BrandImport);
        Assert.Contains("\"code\":\"NEW-BRAND\"", stored.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecutePendingAsync_CompletesBrandImportJobWithCreateUpdateAndRowErrors()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var payload = new BrandImportJobPayload(
            [
                new BrandImportJobPayloadItem(
                    "ACME",
                    "https://www.example.com/updated",
                    Guid.Parse("74000000-0000-0000-0000-000000000001"),
                    15,
                    [
                        new BrandImportJobPayloadTranslation("en-GB", "Acme Tools Updated", "acme-tools", "Updated description")
                    ]),
                new BrandImportJobPayloadItem(
                    "NEW-BRAND",
                    "https://example.com/new",
                    null,
                    30,
                    [
                        new BrandImportJobPayloadTranslation("en-GB", "New Brand", "new-brand", "Created by import")
                    ]),
                new BrandImportJobPayloadItem(
                    "",
                    null,
                    null,
                    0,
                    [])
            ]);
        var job = new IntegrationJob(
            Guid.NewGuid(),
            IntegrationJobTypes.BrandImport,
            IntegrationJobDirections.Import,
            "catalog-admin",
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            now);
        store.IntegrationJobs[job.Id] = job;

        var service = CreateExecutionService(store);
        var executed = await service.ExecutePendingAsync(5, CancellationToken.None);

        Assert.Equal(1, executed);

        var completed = store.IntegrationJobs[job.Id];
        Assert.Equal(IntegrationJobStatuses.Completed, completed.Status);
        Assert.Equal(1, completed.AttemptCount);
        Assert.Equal("Imported 3 brands: 1 created, 1 updated, 1 failed.", completed.ResultSummary);

        var acme = store.Brands.Values.Single(x => x.Code == "ACME");
        Assert.Equal("https://www.example.com/updated", acme.WebsiteUrl);
        Assert.Equal(15, acme.SortOrder);
        Assert.Equal("Acme Tools Updated", acme.Translations.Single(x => x.CultureCode == "en-GB").Name);

        var imported = store.Brands.Values.Single(x => x.Code == "NEW-BRAND");
        Assert.Equal("https://example.com/new", imported.WebsiteUrl);
        Assert.Equal("New Brand", imported.Translations.Single(x => x.CultureCode == "en-GB").Name);

        var result = JsonSerializer.Deserialize<BrandImportJobResult>(completed.ResultPayloadJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(result);
        Assert.Equal(1, result!.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal("Failed", result.Items.Single(x => x.RowNumber == 3).Outcome);
    }

    [Fact]
    public async Task CreateProductExportJob_PersistsPendingJobForCurrentActor()
    {
        var store = new InMemoryCatalogStore();
        var service = new IntegrationJobAdminApplicationService(
            new InMemoryIntegrationJobRepository(store),
            new StubCurrentActorAccessor("catalog-admin"),
            new InMemoryUnitOfWork());

        var created = await service.CreateProductExportAsync(
            new CreateProductExportJobCommand("SKU", "Active", "READY", null, true),
            CancellationToken.None);

        Assert.Equal(IntegrationJobTypes.ProductExport, created.Type);
        Assert.Equal(IntegrationJobDirections.Export, created.Direction);
        Assert.Equal(IntegrationJobStatuses.Pending, created.Status);
        Assert.Equal("catalog-admin", created.RequestedBy);

        var stored = Assert.Single(store.IntegrationJobs.Values, x => x.Type == IntegrationJobTypes.ProductExport);
        Assert.Contains("\"productStatusCode\":\"READY\"", stored.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecutePendingAsync_CompletesProductExportJobAndStoresDetailedResult()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var job = new IntegrationJob(
            Guid.NewGuid(),
            IntegrationJobTypes.ProductExport,
            IntegrationJobDirections.Export,
            "catalog-admin",
            JsonSerializer.Serialize(new ProductExportJobPayload("SKU", "Active", "READY", null, true), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            now);
        store.IntegrationJobs[job.Id] = job;

        var service = CreateExecutionService(store);
        var executed = await service.ExecutePendingAsync(5, CancellationToken.None);

        Assert.Equal(1, executed);

        var completed = store.IntegrationJobs[job.Id];
        Assert.Equal(IntegrationJobStatuses.Completed, completed.Status);
        Assert.Equal("Exported 1 products.", completed.ResultSummary);

        var result = JsonSerializer.Deserialize<ProductExportJobResult>(completed.ResultPayloadJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalCount);

        var product = Assert.Single(result.Items);
        Assert.Equal("SKU-EXAMPLE-1", product.ProductNumber);
        Assert.Equal("Acme Tools", product.BrandName);
        Assert.Equal("READY", product.ProductStatus.Code);
        Assert.True(product.HasVariants);
        Assert.Contains(product.Categories, x => x.Code == "DRILLS");
        Assert.Contains(product.AttributeValues, x => x.ProductAttributeCode == "POWER_SOURCE");
        Assert.Contains(product.Media, x => x.PublicUrl == "https://images.example.com/drill-hero.jpg");
        Assert.Empty(product.Relations);
        Assert.Contains(product.Translations, x => x.CultureCode == "en-GB" && x.Name == "Example Drill");
    }

    [Fact]
    public async Task CreateProductImportJob_PersistsPendingJobForCurrentActor()
    {
        var store = new InMemoryCatalogStore();
        var service = new IntegrationJobAdminApplicationService(
            new InMemoryIntegrationJobRepository(store),
            new StubCurrentActorAccessor("catalog-admin"),
            new InMemoryUnitOfWork());

        var created = await service.CreateProductImportAsync(
            new CreateProductImportJobCommand(
                [
                    new ProductImportJobItemInput(
                        "Hardware",
                        "SKU-NEW-IMPORT-1",
                        "sku-new-import-1",
                        "ACME",
                        "READY",
                        "STANDARD",
                        "pcs",
                        true,
                        1.2m,
                        10m,
                        5m,
                        3m,
                        ["DRILLS"],
                        [
                            new ProductImportJobAttributeValueInput("POWER_SOURCE", "CORDLESS", null)
                        ],
                        [
                            new ProductImportJobTranslationInput("en-GB", "Imported Product", "Short", "Long", "SEO", "SEO description")
                        ])
                ]),
            CancellationToken.None);

        Assert.Equal(IntegrationJobTypes.ProductImport, created.Type);
        Assert.Equal(IntegrationJobDirections.Import, created.Direction);
        Assert.Equal(IntegrationJobStatuses.Pending, created.Status);
        Assert.Equal("catalog-admin", created.RequestedBy);

        var stored = Assert.Single(store.IntegrationJobs.Values, x => x.Type == IntegrationJobTypes.ProductImport);
        Assert.Contains("\"productNumber\":\"SKU-NEW-IMPORT-1\"", stored.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateStorefrontProjectionRebuildJob_PersistsPendingJobForCurrentActor()
    {
        var store = new InMemoryCatalogStore();
        var service = new IntegrationJobAdminApplicationService(
            new InMemoryIntegrationJobRepository(store),
            new StubCurrentActorAccessor("catalog-admin"),
            new InMemoryUnitOfWork());

        var created = await service.CreateStorefrontProjectionRebuildAsync(
            new CreateStorefrontProjectionRebuildJobCommand(),
            CancellationToken.None);

        Assert.Equal(IntegrationJobTypes.StorefrontProjectionRebuild, created.Type);
        Assert.Equal(IntegrationJobDirections.Rebuild, created.Direction);
        Assert.Equal(IntegrationJobStatuses.Pending, created.Status);
        Assert.Equal("catalog-admin", created.RequestedBy);
    }

    [Fact]
    public async Task ExecutePendingAsync_CompletesProductImportJobWithCreateUpdateAndRowErrors()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var payload = new ProductImportJobPayload(
            [
                new ProductImportJobPayloadItem(
                    "Hardware",
                    "SKU-EXAMPLE-1",
                    "example-drill-updated",
                    "ACME",
                    "READY",
                    "STANDARD",
                    "pcs",
                    true,
                    2.1m,
                    30m,
                    9m,
                    23m,
                    ["DRILLS"],
                    [
                        new ProductImportJobPayloadAttributeValue("POWER_SOURCE", "CORDLESS", null)
                    ],
                    [
                        new ProductImportJobPayloadTranslation("en-GB", "Example Drill Updated", "Short", "Long", "SEO", "SEO description")
                    ]),
                new ProductImportJobPayloadItem(
                    "Hardware",
                    "SKU-NEW-IMPORT-1",
                    "new-import-drill",
                    "ACME",
                    "READY",
                    "STANDARD",
                    "pcs",
                    false,
                    1.0m,
                    20m,
                    7m,
                    15m,
                    ["TOOLS"],
                    [
                        new ProductImportJobPayloadAttributeValue("POWER_SOURCE", "CORDED", null)
                    ],
                    [
                        new ProductImportJobPayloadTranslation("en-GB", "New Import Drill", null, null, null, null)
                    ]),
                new ProductImportJobPayloadItem(
                    "Hardware",
                    "SKU-BAD-1",
                    "bad-import-drill",
                    "ACME",
                    "UNKNOWN",
                    "STANDARD",
                    "pcs",
                    false,
                    null,
                    null,
                    null,
                    null,
                    [],
                    [],
                    [])
            ]);
        var job = new IntegrationJob(
            Guid.NewGuid(),
            IntegrationJobTypes.ProductImport,
            IntegrationJobDirections.Import,
            "catalog-admin",
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            now);
        store.IntegrationJobs[job.Id] = job;

        var service = CreateExecutionService(store);
        var executed = await service.ExecutePendingAsync(5, CancellationToken.None);

        Assert.Equal(1, executed);

        var completed = store.IntegrationJobs[job.Id];
        Assert.Equal(IntegrationJobStatuses.Completed, completed.Status);
        Assert.Equal("Imported 3 products: 1 created, 1 updated, 1 failed.", completed.ResultSummary);

        var existing = store.Products.Values.Single(x => x.ProductNumber == "SKU-EXAMPLE-1");
        Assert.Equal("example-drill-updated", existing.Slug);
        Assert.Equal(2.1m, existing.Weight);
        Assert.Equal("Example Drill Updated", existing.Translations.Single(x => x.CultureCode == "en-GB").Name);

        var created = store.Products.Values.Single(x => x.ProductNumber == "SKU-NEW-IMPORT-1");
        Assert.Equal("new-import-drill", created.Slug);
        Assert.False(created.HasVariants);
        Assert.Equal("New Import Drill", created.Translations.Single().Name);

        var result = JsonSerializer.Deserialize<ProductImportJobResult>(completed.ResultPayloadJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(result);
        Assert.Equal(1, result!.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal("Failed", result.Items.Single(x => x.RowNumber == 3).Outcome);
    }

    [Fact]
    public async Task ExecutePendingAsync_ProductImportTreatsSlugCollisionAsRowFailureInsteadOfCrashingJob()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var payload = new ProductImportJobPayload(
            [
                new ProductImportJobPayloadItem(
                    "Hardware",
                    "SKU-NEW-CONFLICT-1",
                    "example-drill",
                    "ACME",
                    "READY",
                    "STANDARD",
                    "pcs",
                    false,
                    null,
                    null,
                    null,
                    null,
                    ["TOOLS"],
                    [],
                    [
                        new ProductImportJobPayloadTranslation("en-GB", "Conflicting Slug Product", null, null, null, null)
                    ])
            ]);
        var job = new IntegrationJob(
            Guid.NewGuid(),
            IntegrationJobTypes.ProductImport,
            IntegrationJobDirections.Import,
            "catalog-admin",
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            now);
        store.IntegrationJobs[job.Id] = job;

        var service = CreateExecutionService(store);
        var executed = await service.ExecutePendingAsync(5, CancellationToken.None);

        Assert.Equal(1, executed);

        var completed = store.IntegrationJobs[job.Id];
        Assert.Equal(IntegrationJobStatuses.Completed, completed.Status);
        Assert.Equal("Imported 1 products: 0 created, 0 updated, 1 failed.", completed.ResultSummary);
        Assert.DoesNotContain(store.Products.Values, x => x.ProductNumber == "SKU-NEW-CONFLICT-1");

        var result = JsonSerializer.Deserialize<ProductImportJobResult>(completed.ResultPayloadJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(result);
        Assert.Equal("Failed", Assert.Single(result!.Items).Outcome);
        Assert.Contains("Slug already exists", Assert.Single(result.Items).Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecutePendingAsync_CompletesStorefrontProjectionRebuildJob()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var job = new IntegrationJob(
            Guid.NewGuid(),
            IntegrationJobTypes.StorefrontProjectionRebuild,
            IntegrationJobDirections.Rebuild,
            "catalog-admin",
            "{}",
            now);
        store.IntegrationJobs[job.Id] = job;

        var service = CreateExecutionService(store);
        var executed = await service.ExecutePendingAsync(5, CancellationToken.None);

        Assert.Equal(1, executed);

        var completed = store.IntegrationJobs[job.Id];
        Assert.Equal(IntegrationJobStatuses.Completed, completed.Status);
        Assert.Contains("Rebuilt", completed.ResultSummary, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(store.StorefrontProductProjections.Values);
    }

    [Fact]
    public async Task JobCompletion_PublishesWebhookEventAndProcessesDelivery()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Integration job sink",
            "https://example.test/hooks/jobs",
            "secret",
            [WebhookEventTypes.IntegrationJobCompleted],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var job = new IntegrationJob(
            Guid.NewGuid(),
            IntegrationJobTypes.ProductExport,
            IntegrationJobDirections.Export,
            "catalog-admin",
            JsonSerializer.Serialize(new ProductExportJobPayload("SKU", "Active", "READY", null, true), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            now);
        store.IntegrationJobs[job.Id] = job;

        var integrationJobExecutionService = CreateExecutionService(store);
        var outboxExecutionService = new WebhookOutboxExecutionService(
            new InMemoryOutboxMessageRepository(store),
            new InMemoryWebhookSubscriptionRepository(store),
            new InMemoryWebhookDeliveryRepository(store),
            new InMemoryUnitOfWork(),
            NullLogger<WebhookOutboxExecutionService>.Instance);
        var fakeSender = new FakeWebhookSender();
        var deliveryExecutionService = new WebhookDeliveryExecutionService(
            new InMemoryWebhookDeliveryRepository(store),
            new InMemoryWebhookSubscriptionRepository(store),
            fakeSender,
            new InMemoryUnitOfWork(),
            NullLogger<WebhookDeliveryExecutionService>.Instance);

        Assert.Equal(1, await integrationJobExecutionService.ExecutePendingAsync(5, CancellationToken.None));
        Assert.Single(store.OutboxMessages.Values);

        Assert.Equal(1, await outboxExecutionService.ExecutePendingAsync(5, CancellationToken.None));
        var outboxMessage = Assert.Single(store.OutboxMessages.Values);
        Assert.True(outboxMessage.IsPublished);

        var delivery = Assert.Single(store.WebhookDeliveries.Values);
        Assert.Equal(WebhookDeliveryStatuses.Pending, delivery.Status);

        Assert.Equal(1, await deliveryExecutionService.ExecutePendingAsync(5, CancellationToken.None));
        Assert.Equal(WebhookDeliveryStatuses.Succeeded, delivery.Status);
        Assert.Single(fakeSender.Requests);
        Assert.Equal(WebhookEventTypes.IntegrationJobCompleted, fakeSender.Requests[0].EventType);
        Assert.Contains(job.Id.ToString(), fakeSender.Requests[0].PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WebhookDeliveryExecution_AbandonsPermanentClientFailures()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Permanent failure sink",
            "https://example.test/hooks/fail",
            "secret",
            [WebhookEventTypes.IntegrationJobCompleted],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var delivery = new WebhookDelivery(
            Guid.NewGuid(),
            subscription.Id,
            Guid.NewGuid(),
            WebhookEventTypes.IntegrationJobCompleted,
            "{\"ok\":true}",
            now);
        store.WebhookDeliveries[delivery.Id] = delivery;

        var service = new WebhookDeliveryExecutionService(
            new InMemoryWebhookDeliveryRepository(store),
            new InMemoryWebhookSubscriptionRepository(store),
            new FixedWebhookSender(new WebhookSendResult(false, 410, "{\"gone\":true}")),
            new InMemoryUnitOfWork(),
            NullLogger<WebhookDeliveryExecutionService>.Instance);

        var processed = await service.ExecutePendingAsync(5, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(WebhookDeliveryStatuses.Abandoned, delivery.Status);
        Assert.Equal(410, delivery.ResponseCode);
        Assert.Null(delivery.NextAttemptAtUtc);
    }

    [Fact]
    public async Task WebhookAdminApplicationService_RejectsUnsupportedEventTypes()
    {
        var service = new WebhookAdminApplicationService(
            new InMemoryWebhookSubscriptionRepository(new InMemoryCatalogStore()),
            new InMemoryWebhookDeliveryRepository(new InMemoryCatalogStore()),
            Options.Create(new WebhookReplayOptions()),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<Platform.Application.Abstractions.Errors.RequestValidationException>(
            () => service.CreateSubscriptionAsync(
                new CreateWebhookSubscriptionCommand(
                    "Bad subscription",
                    "https://example.test/hooks/bad",
                    "secret",
                    ["unsupported.event"],
                    true),
                CancellationToken.None));
    }

    private static IntegrationJobExecutionService CreateExecutionService(InMemoryCatalogStore store)
    {
        var projectionRepository = new InMemoryStorefrontProductProjectionRepository(store);
        var projectionRefreshService = new StorefrontProjectionRefreshService(
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

        return new IntegrationJobExecutionService(
            new InMemoryIntegrationJobRepository(store),
            new InMemoryBrandRepository(store),
            new InMemoryCategoryRepository(store),
            new InMemoryProductAttributeRepository(store),
            new InMemoryMediaAssetRepository(store),
            new InMemoryProductRepository(store),
            new InMemoryProductStatusDefinitionRepository(store),
            projectionRepository,
            projectionRefreshService,
            new OutboxEventPublisher(new InMemoryOutboxMessageRepository(store)),
            new InMemoryUnitOfWork(),
            NullLogger<IntegrationJobExecutionService>.Instance);
    }

    private sealed class StubCurrentActorAccessor : ICurrentActorAccessor
    {
        private readonly string _identifier;

        public StubCurrentActorAccessor(string identifier)
        {
            _identifier = identifier;
        }

        public AuthenticatedActor GetCurrentActor()
        {
            return new AuthenticatedActor(_identifier, _identifier, "AdminUser", [], true);
        }
    }

    private sealed class FakeWebhookSender : IWebhookSender
    {
        public List<FakeWebhookRequest> Requests { get; } = [];

        public Task<WebhookSendResult> SendAsync(WebhookSubscription subscription, WebhookDelivery delivery, CancellationToken cancellationToken)
        {
            Requests.Add(new FakeWebhookRequest(subscription.EndpointUrl, delivery.EventType, delivery.PayloadJson));
            return Task.FromResult(new WebhookSendResult(true, 200, "{\"ok\":true}"));
        }
    }

    private sealed class FixedWebhookSender : IWebhookSender
    {
        private readonly WebhookSendResult _result;

        public FixedWebhookSender(WebhookSendResult result)
        {
            _result = result;
        }

        public Task<WebhookSendResult> SendAsync(WebhookSubscription subscription, WebhookDelivery delivery, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed record FakeWebhookRequest(string EndpointUrl, string EventType, string PayloadJson);
}
