using Microsoft.Extensions.Options;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Integrations.Commands;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Persistence;

namespace Platform.Tests;

public sealed class WebhookReplayTests
{
    [Fact]
    public async Task ReplayDeliveryAsync_RejectsReplayWhenManualReplayIsDisabled()
    {
        var store = new InMemoryCatalogStore();
        var delivery = CreateFailedDelivery(store);
        var service = CreateService(store, manualReplayEnabled: false, manualReplayDelaySeconds: 60);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.ReplayDeliveryAsync(
                new ReplayWebhookDeliveryCommand(delivery.Id, delivery.RowVersion),
                CancellationToken.None));

        Assert.Contains(nameof(WebhookReplayOptions.ManualReplayEnabled), exception.Errors.Keys);
    }

    [Fact]
    public async Task ReplayDeliveryAsync_SchedulesFailedDeliveryUsingConfiguredDelay()
    {
        var store = new InMemoryCatalogStore();
        var delivery = CreateAbandonedDelivery(store);
        var originalRowVersion = delivery.RowVersion;
        var service = CreateService(store, manualReplayEnabled: true, manualReplayDelaySeconds: 90);
        var lowerBound = DateTime.UtcNow.AddSeconds(90);

        var replayed = await service.ReplayDeliveryAsync(
            new ReplayWebhookDeliveryCommand(delivery.Id, delivery.RowVersion),
            CancellationToken.None);

        var upperBound = DateTime.UtcNow.AddSeconds(90);

        Assert.NotNull(replayed);
        Assert.Equal(WebhookDeliveryStatuses.Failed, replayed!.Status);
        Assert.NotNull(replayed.NextAttemptAtUtc);
        Assert.InRange(replayed.NextAttemptAtUtc!.Value, lowerBound.AddSeconds(-1), upperBound.AddSeconds(1));
        Assert.Equal(WebhookDeliveryStatuses.Failed, delivery.Status);
        Assert.NotEqual(originalRowVersion, replayed.RowVersion);
    }

    private static WebhookAdminApplicationService CreateService(InMemoryCatalogStore store, bool manualReplayEnabled, int manualReplayDelaySeconds)
    {
        return new WebhookAdminApplicationService(
            new InMemoryWebhookSubscriptionRepository(store),
            new InMemoryWebhookDeliveryRepository(store),
            Options.Create(new WebhookReplayOptions
            {
                ManualReplayEnabled = manualReplayEnabled,
                ManualReplayDelaySeconds = manualReplayDelaySeconds
            }),
            new InMemoryUnitOfWork());
    }

    private static WebhookDelivery CreateFailedDelivery(InMemoryCatalogStore store)
    {
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Replay subscription",
            "https://example.test/hooks/replay",
            "secret",
            [WebhookEventTypes.ProductUpdated],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var delivery = new WebhookDelivery(
            Guid.NewGuid(),
            subscription.Id,
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "{\"event\":\"product.updated\"}",
            now);
        delivery.Start(delivery.RowVersion);
        delivery.MarkFailed(500, "{\"error\":true}", now.AddMinutes(5), delivery.RowVersion);
        store.WebhookDeliveries[delivery.Id] = delivery;
        return delivery;
    }

    private static WebhookDelivery CreateAbandonedDelivery(InMemoryCatalogStore store)
    {
        var now = DateTime.UtcNow;
        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            "Replay subscription",
            "https://example.test/hooks/replay",
            "secret",
            [WebhookEventTypes.ProductUpdated],
            true,
            now);
        store.WebhookSubscriptions[subscription.Id] = subscription;

        var delivery = new WebhookDelivery(
            Guid.NewGuid(),
            subscription.Id,
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "{\"event\":\"product.updated\"}",
            now);
        delivery.Start(delivery.RowVersion);
        delivery.MarkAbandoned(410, "{\"gone\":true}", delivery.RowVersion);
        store.WebhookDeliveries[delivery.Id] = delivery;
        return delivery;
    }
}
