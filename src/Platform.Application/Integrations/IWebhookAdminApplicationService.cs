using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;

namespace Platform.Application.Integrations;

public interface IWebhookAdminApplicationService
{
    Task<PagedResponse<WebhookSubscriptionSummaryDto>> ListSubscriptionsAsync(ListWebhookSubscriptionsQuery query, CancellationToken cancellationToken);
    Task<WebhookSubscriptionDetailsDto?> GetSubscriptionByIdAsync(GetWebhookSubscriptionByIdQuery query, CancellationToken cancellationToken);
    Task<WebhookSubscriptionDetailsDto> CreateSubscriptionAsync(CreateWebhookSubscriptionCommand command, CancellationToken cancellationToken);
    Task<WebhookSubscriptionDetailsDto?> UpdateSubscriptionAsync(UpdateWebhookSubscriptionCommand command, CancellationToken cancellationToken);
    Task<PagedResponse<WebhookDeliverySummaryDto>> ListDeliveriesAsync(ListWebhookDeliveriesQuery query, CancellationToken cancellationToken);
    Task<WebhookDeliveryDetailsDto?> GetDeliveryByIdAsync(GetWebhookDeliveryByIdQuery query, CancellationToken cancellationToken);
    Task<WebhookDeliveryDetailsDto?> ReplayDeliveryAsync(ReplayWebhookDeliveryCommand command, CancellationToken cancellationToken);
}
