namespace Platform.Application.Integrations;

public interface IWebhookOutboxExecutionService
{
    Task<int> ExecutePendingAsync(int maxMessages, CancellationToken cancellationToken);
}
