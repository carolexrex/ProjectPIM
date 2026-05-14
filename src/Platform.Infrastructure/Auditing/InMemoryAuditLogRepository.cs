using Platform.Application.Auditing;
using Platform.Application.Auditing.Queries;
using Platform.Domain.Auditing;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Auditing;

public sealed class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryAuditLogRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<AuditLogListResult> ListAsync(ListAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _store.AuditLogs.Values
            .Where(x => string.IsNullOrWhiteSpace(query.EntityType) || string.Equals(x.EntityType, query.EntityType, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.ActorIdentifier) || x.ActorIdentifier.Contains(query.ActorIdentifier, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.Action) || string.Equals(x.Action, query.Action, StringComparison.OrdinalIgnoreCase))
            .Where(x => !query.OccurredFromUtc.HasValue || x.OccurredAtUtc >= query.OccurredFromUtc.Value)
            .Where(x => !query.OccurredToUtc.HasValue || x.OccurredAtUtc <= query.OccurredToUtc.Value);

        filtered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "occurredatutc" => filtered.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),
            _ => filtered.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
        };

        var materialized = filtered.ToList();
        return Task.FromResult(new AuditLogListResult(materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(), materialized.Count));
    }

    public Task<AuditLog?> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken)
    {
        _store.AuditLogs.TryGetValue(auditLogId, out var auditLog);
        return Task.FromResult(auditLog);
    }

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        _store.AuditLogs[auditLog.Id] = auditLog;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IReadOnlyCollection<AuditLog> auditLogs, CancellationToken cancellationToken)
    {
        foreach (var auditLog in auditLogs)
        {
            _store.AuditLogs[auditLog.Id] = auditLog;
        }

        return Task.CompletedTask;
    }
}
