using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IIntegrationJobRepository
{
    Task<IntegrationJobListResult> ListAsync(ListIntegrationJobsQuery query, CancellationToken cancellationToken);
    Task<IntegrationJob?> GetByIdAsync(Guid integrationJobId, CancellationToken cancellationToken);
    Task<IntegrationJob?> GetNextRunnableAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task AddAsync(IntegrationJob integrationJob, CancellationToken cancellationToken);
}
