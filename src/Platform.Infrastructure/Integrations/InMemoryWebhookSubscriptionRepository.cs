using Platform.Application.Integrations;
using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Integrations;

public sealed class InMemoryWebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryWebhookSubscriptionRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<WebhookSubscriptionListResult> ListAsync(ListWebhookSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
                _store.WebhookSubscriptions.Values
                    .Where(x => string.IsNullOrWhiteSpace(query.Search)
                        || x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                        || x.EndpointUrl.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                    .Where(x => query.IsActive is null || x.IsActive == query.IsActive)
                    .Where(x => string.IsNullOrWhiteSpace(query.EventType) || x.SupportsEventType(query.EventType)),
                query.Sort)
            .ToList();

        return Task.FromResult(new WebhookSubscriptionListResult(
            filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            filtered.Count));
    }

    public Task<IReadOnlyList<WebhookSubscription>> ListActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<WebhookSubscription> items = _store.WebhookSubscriptions.Values
            .Where(x => x.SupportsEventType(eventType))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<WebhookSubscription?> GetByIdAsync(Guid webhookSubscriptionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.WebhookSubscriptions.TryGetValue(webhookSubscriptionId, out var subscription) ? subscription : null);
    }

    public Task AddAsync(WebhookSubscription webhookSubscription, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.WebhookSubscriptions[webhookSubscription.Id] = webhookSubscription;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<WebhookSubscription> ApplySorting(IEnumerable<WebhookSubscription> subscriptions, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "name" => subscriptions.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
            "-updatedatutc" => subscriptions.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id),
            "updatedatutc" => subscriptions.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            _ => subscriptions.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
