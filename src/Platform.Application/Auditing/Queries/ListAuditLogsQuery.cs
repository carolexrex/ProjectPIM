namespace Platform.Application.Auditing.Queries;

public sealed record ListAuditLogsQuery(
    string? EntityType,
    string? ActorIdentifier,
    string? Action,
    DateTime? OccurredFromUtc,
    DateTime? OccurredToUtc,
    int Page,
    int PageSize,
    string? Sort);
