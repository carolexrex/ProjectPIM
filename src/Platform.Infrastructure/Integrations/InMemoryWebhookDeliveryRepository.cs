using Platform.Application.Integrations;
using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Integrations;

public sealed class InMemoryWebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryWebhookDeliveryRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<WebhookDeliveryListResult> ListAsync(ListWebhookDeliveriesQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
                _store.WebhookDeliveries.Values
                    .Where(x => query.WebhookSubscriptionId is null || x.WebhookSubscriptionId == query.WebhookSubscriptionId)
                    .Where(x => string.IsNullOrWhiteSpace(query.EventType)
                        || string.Equals(x.EventType, query.EventType, StringComparison.OrdinalIgnoreCase))
                    .Where(x => string.IsNullOrWhiteSpace(query.Status)
                        || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase)),
                query.Sort)
            .ToList();

        return Task.FromResult(new WebhookDeliveryListResult(
            filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            filtered.Count));
    }

    public Task<WebhookDelivery?> GetByIdAsync(Guid webhookDeliveryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.WebhookDeliveries.TryGetValue(webhookDeliveryId, out var delivery) ? delivery : null);
    }

    public Task<WebhookDelivery?> GetNextRunnableAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var delivery = _store.WebhookDeliveries.Values
            .Where(x => x.CanAttemptAt(nowUtc))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefault();
        return Task.FromResult(delivery);
    }

    public Task AddAsync(WebhookDelivery webhookDelivery, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.WebhookDeliveries[webhookDelivery.Id] = webhookDelivery;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<WebhookDelivery> ApplySorting(IEnumerable<WebhookDelivery> deliveries, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "updatedatutc" => deliveries.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            "-updatedatutc" => deliveries.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id),
            _ => deliveries.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
