using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public interface IWebhookSender
{
    Task<WebhookSendResult> SendAsync(
        WebhookSubscription subscription,
        WebhookDelivery delivery,
        CancellationToken cancellationToken);
}
