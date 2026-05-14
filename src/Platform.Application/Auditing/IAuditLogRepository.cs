using Platform.Application.Auditing.Queries;
using Platform.Domain.Auditing;

namespace Platform.Application.Auditing;

public interface IAuditLogRepository
{
    Task<AuditLogListResult> ListAsync(ListAuditLogsQuery query, CancellationToken cancellationToken);
    Task<AuditLog?> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken);
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task AddRangeAsync(IReadOnlyCollection<AuditLog> auditLogs, CancellationToken cancellationToken);
}
