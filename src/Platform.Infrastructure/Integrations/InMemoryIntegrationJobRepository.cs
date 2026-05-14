using Platform.Application.Integrations;
using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Integrations;

public sealed class InMemoryIntegrationJobRepository : IIntegrationJobRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryIntegrationJobRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<IntegrationJobListResult> ListAsync(ListIntegrationJobsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
                _store.IntegrationJobs.Values
                    .Where(job => string.IsNullOrWhiteSpace(query.Type)
                        || string.Equals(job.Type, query.Type, StringComparison.OrdinalIgnoreCase))
                    .Where(job => string.IsNullOrWhiteSpace(query.Status)
                        || string.Equals(job.Status, query.Status, StringComparison.OrdinalIgnoreCase))
                    .Where(job => string.IsNullOrWhiteSpace(query.RequestedBy)
                        || job.RequestedBy.Contains(query.RequestedBy, StringComparison.OrdinalIgnoreCase)),
                query.Sort)
            .ToList();

        return Task.FromResult(new IntegrationJobListResult(
            filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            filtered.Count));
    }

    public Task<IntegrationJob?> GetByIdAsync(Guid integrationJobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.IntegrationJobs.TryGetValue(integrationJobId, out var job) ? job : null);
    }

    public Task<IntegrationJob?> GetNextRunnableAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var job = _store.IntegrationJobs.Values
            .Where(x => x.CanStartAt(nowUtc))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        return Task.FromResult(job);
    }

    public Task AddAsync(IntegrationJob integrationJob, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.IntegrationJobs[integrationJob.Id] = integrationJob;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<IntegrationJob> ApplySorting(IEnumerable<IntegrationJob> jobs, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "createdatutc" => jobs.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            "updatedatutc" => jobs.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            "-updatedatutc" => jobs.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id),
            _ => jobs.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
