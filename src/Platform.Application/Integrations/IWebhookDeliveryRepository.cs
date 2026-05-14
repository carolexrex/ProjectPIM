using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IWebhookDeliveryRepository
{
    Task<WebhookDeliveryListResult> ListAsync(ListWebhookDeliveriesQuery query, CancellationToken cancellationToken);
    Task<WebhookDelivery?> GetByIdAsync(Guid webhookDeliveryId, CancellationToken cancellationToken);
    Task<WebhookDelivery?> GetNextRunnableAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task AddAsync(WebhookDelivery webhookDelivery, CancellationToken cancellationToken);
}
