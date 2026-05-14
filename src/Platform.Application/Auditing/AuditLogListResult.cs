using Platform.Domain.Auditing;

namespace Platform.Application.Auditing;

public sealed record AuditLogListResult(
    IReadOnlyList<AuditLog> Items,
    int Total);
