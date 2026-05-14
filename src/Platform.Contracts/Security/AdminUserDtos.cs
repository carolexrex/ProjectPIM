namespace Platform.Contracts.Security;

public sealed record AdminUserSummaryDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record AdminUserDetailsDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record AuditLogSummaryDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string ActorIdentifier,
    string ActorDisplayName,
    string ActorType,
    DateTime OccurredAtUtc);

public sealed record AuditLogDetailsDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string ActorIdentifier,
    string ActorDisplayName,
    string ActorType,
    IReadOnlyList<string> ChangedFields,
    DateTime OccurredAtUtc);
