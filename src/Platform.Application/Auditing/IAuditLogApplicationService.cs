using Platform.Application.Auditing.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Security;

namespace Platform.Application.Auditing;

public interface IAuditLogApplicationService
{
    Task<PagedResponse<AuditLogSummaryDto>> ListAsync(ListAuditLogsQuery query, CancellationToken cancellationToken);
    Task<AuditLogDetailsDto?> GetByIdAsync(GetAuditLogByIdQuery query, CancellationToken cancellationToken);
}
