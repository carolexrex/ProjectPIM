namespace Platform.Application.Integrations;

public interface IWebhookDeliveryExecutionService
{
    Task<int> ExecutePendingAsync(int maxDeliveries, CancellationToken cancellationToken);
}
