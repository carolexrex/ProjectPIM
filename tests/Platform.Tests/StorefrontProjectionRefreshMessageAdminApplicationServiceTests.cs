using Platform.Application.Abstractions.Errors;
using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Contracts.Integrations;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Persistence;

namespace Platform.Tests;

public sealed class StorefrontProjectionRefreshMessageAdminApplicationServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsOnlyStorefrontRefreshMessagesWithProcessingStatus()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var pending = CreateRefreshMessage(now.AddMinutes(-3));
        var delayed = CreateRefreshMessage(now.AddMinutes(-2));
        var abandoned = CreateRefreshMessage(now.AddMinutes(-1));
        var unrelated = new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.ProductUpdated,
            "Product",
            Guid.NewGuid(),
            "{\"event\":\"product.updated\"}",
            now);

        delayed.MarkProcessingRetry("temporary failure", now.AddMinutes(30), delayed.RowVersion);
        abandoned.MarkProcessingAbandoned("permanent failure", abandoned.RowVersion);
        store.OutboxMessages[pending.Id] = pending;
        store.OutboxMessages[delayed.Id] = delayed;
        store.OutboxMessages[abandoned.Id] = abandoned;
        store.OutboxMessages[unrelated.Id] = unrelated;
        var service = CreateService(store);

        var response = await service.ListAsync(
            new ListStorefrontProjectionRefreshMessagesQuery("open", 1, 50, "occurredAtUtc"),
            CancellationToken.None);

        Assert.Equal(3, response.Total);
        Assert.DoesNotContain(response.Items, x => x.Id == unrelated.Id);
        Assert.Contains(response.Items, x => x.Id == pending.Id && x.Status == StorefrontProjectionRefreshMessageStatuses.Pending);
        Assert.Contains(response.Items, x => x.Id == delayed.Id && x.Status == StorefrontProjectionRefreshMessageStatuses.Delayed);
        Assert.Contains(response.Items, x => x.Id == abandoned.Id && x.Status == StorefrontProjectionRefreshMessageStatuses.Abandoned);
    }

    [Fact]
    public async Task ResetAsync_ClearsAbandonedStateAndMakesMessagePending()
    {
        var store = new InMemoryCatalogStore();
        var message = CreateRefreshMessage(DateTime.UtcNow);
        message.MarkProcessingAbandoned("projection failure", message.RowVersion);
        store.OutboxMessages[message.Id] = message;
        var originalRowVersion = message.RowVersion;
        var service = CreateService(store);

        var reset = await service.ResetAsync(
            new ResetStorefrontProjectionRefreshMessageCommand(message.Id, message.RowVersion),
            CancellationToken.None);

        Assert.NotNull(reset);
        Assert.Equal(StorefrontProjectionRefreshMessageStatuses.Pending, reset!.Status);
        Assert.Equal(0, reset.ProcessingAttemptCount);
        Assert.Null(reset.LastProcessingError);
        Assert.Null(reset.NextProcessingAttemptAtUtc);
        Assert.Null(reset.ProcessingAbandonedAtUtc);
        Assert.NotEqual(originalRowVersion, reset.RowVersion);
    }

    [Fact]
    public async Task ResetAsync_RejectsMessagesThatAreNotAbandoned()
    {
        var store = new InMemoryCatalogStore();
        var message = CreateRefreshMessage(DateTime.UtcNow);
        store.OutboxMessages[message.Id] = message;
        var service = CreateService(store);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.ResetAsync(
                new ResetStorefrontProjectionRefreshMessageCommand(message.Id, message.RowVersion),
                CancellationToken.None));

        Assert.Contains("OutboxMessageId", exception.Errors.Keys);
    }

    private static StorefrontProjectionRefreshMessageAdminApplicationService CreateService(InMemoryCatalogStore store)
    {
        return new StorefrontProjectionRefreshMessageAdminApplicationService(
            new InMemoryOutboxMessageRepository(store),
            new InMemoryUnitOfWork());
    }

    private static OutboxMessage CreateRefreshMessage(DateTime occurredAtUtc)
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            "Product",
            Guid.NewGuid(),
            "{\"aggregateType\":\"Product\"}",
            occurredAtUtc);
    }
}
