namespace Platform.Application.Integrations;

public interface IIntegrationJobExecutionService
{
    Task<int> ExecutePendingAsync(int maxJobs, CancellationToken cancellationToken);
}
