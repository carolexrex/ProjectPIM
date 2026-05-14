using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IWebhookSubscriptionRepository
{
    Task<WebhookSubscriptionListResult> ListAsync(ListWebhookSubscriptionsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<WebhookSubscription>> ListActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken);
    Task<WebhookSubscription?> GetByIdAsync(Guid webhookSubscriptionId, CancellationToken cancellationToken);
    Task AddAsync(WebhookSubscription webhookSubscription, CancellationToken cancellationToken);
}
