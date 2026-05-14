using Microsoft.EntityFrameworkCore;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Integrations;

public sealed class EfIntegrationJobRepository : IIntegrationJobRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfIntegrationJobRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IntegrationJobListResult> ListAsync(ListIntegrationJobsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.IntegrationJobs
            .AsNoTracking()
            .Where(job => string.IsNullOrWhiteSpace(query.Type) || job.Type == query.Type)
            .Where(job => string.IsNullOrWhiteSpace(query.Status) || job.Status == query.Status)
            .Where(job => string.IsNullOrWhiteSpace(query.RequestedBy) || job.RequestedBy.Contains(query.RequestedBy));

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new IntegrationJobListResult(items, total);
    }

    public async Task<IntegrationJob?> GetByIdAsync(Guid integrationJobId, CancellationToken cancellationToken)
    {
        return await _dbContext.IntegrationJobs.FirstOrDefaultAsync(x => x.Id == integrationJobId, cancellationToken);
    }

    public async Task<IntegrationJob?> GetNextRunnableAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await _dbContext.IntegrationJobs
            .Where(x => x.Status == IntegrationJobStatuses.Pending
                || (x.Status == IntegrationJobStatuses.Failed && (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc.Value <= nowUtc)))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(IntegrationJob integrationJob, CancellationToken cancellationToken)
    {
        await _dbContext.IntegrationJobs.AddAsync(integrationJob, cancellationToken);
    }

    private static IQueryable<IntegrationJob> ApplySorting(IQueryable<IntegrationJob> jobs, string? sort)
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
