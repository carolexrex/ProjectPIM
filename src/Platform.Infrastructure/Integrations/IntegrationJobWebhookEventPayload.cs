namespace Platform.Infrastructure.Integrations;

public sealed record IntegrationJobWebhookEventPayload(
    Guid JobId,
    string JobType,
    string Direction,
    string Status,
    string RequestedBy,
    int AttemptCount,
    string? ResultSummary,
    string? LastError,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);
