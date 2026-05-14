namespace Platform.Contracts.Integrations;

public sealed record IntegrationJobSummaryDto(
    Guid Id,
    string Type,
    string Direction,
    string Status,
    string RequestedBy,
    int AttemptCount,
    string? ResultSummary,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record IntegrationJobDetailsDto(
    Guid Id,
    string Type,
    string Direction,
    string Status,
    string RequestedBy,
    string? PayloadJson,
    string? ResultPayloadJson,
    string? ResultSummary,
    string? LastError,
    int AttemptCount,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? NextAttemptAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
