using System.Text.Json;
using Platform.Application.Auditing;
using Platform.Application.Auditing.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Security;
using Platform.Domain.Auditing;

namespace Platform.Infrastructure.Auditing;

public sealed class AuditLogApplicationService : IAuditLogApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogApplicationService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResponse<AuditLogSummaryDto>> ListAsync(ListAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var result = await _auditLogRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<AuditLogSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<AuditLogDetailsDto?> GetByIdAsync(GetAuditLogByIdQuery query, CancellationToken cancellationToken)
    {
        var auditLog = await _auditLogRepository.GetByIdAsync(query.AuditLogId, cancellationToken);
        return auditLog is null ? null : MapDetails(auditLog);
    }

    private static AuditLogSummaryDto MapSummary(AuditLog auditLog)
    {
        return new AuditLogSummaryDto(
            auditLog.Id,
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.Action,
            auditLog.ActorIdentifier,
            auditLog.ActorDisplayName,
            auditLog.ActorType,
            auditLog.OccurredAtUtc);
    }

    private static AuditLogDetailsDto MapDetails(AuditLog auditLog)
    {
        var changedFields = JsonSerializer.Deserialize<IReadOnlyList<string>>(auditLog.ChangedFieldsJson, JsonOptions) ?? [];
        return new AuditLogDetailsDto(
            auditLog.Id,
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.Action,
            auditLog.ActorIdentifier,
            auditLog.ActorDisplayName,
            auditLog.ActorType,
            changedFields,
            auditLog.OccurredAtUtc);
    }
}
