using Microsoft.EntityFrameworkCore;
using Platform.Application.Auditing;
using Platform.Application.Auditing.Queries;
using Platform.Domain.Auditing;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Auditing;

public sealed class EfAuditLogRepository : IAuditLogRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfAuditLogRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLogListResult> ListAsync(ListAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.EntityType) || x.EntityType == query.EntityType)
            .Where(x => string.IsNullOrWhiteSpace(query.ActorIdentifier) || x.ActorIdentifier.Contains(query.ActorIdentifier))
            .Where(x => string.IsNullOrWhiteSpace(query.Action) || x.Action == query.Action)
            .Where(x => !query.OccurredFromUtc.HasValue || x.OccurredAtUtc >= query.OccurredFromUtc.Value)
            .Where(x => !query.OccurredToUtc.HasValue || x.OccurredAtUtc <= query.OccurredToUtc.Value);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AuditLogListResult(items, total);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == auditLogId, cancellationToken);
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<AuditLog> auditLogs, CancellationToken cancellationToken)
    {
        await _dbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
    }

    private static IQueryable<AuditLog> ApplySorting(IQueryable<AuditLog> auditLogs, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "occurredatutc" => auditLogs.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),
            _ => auditLogs.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
