namespace Platform.Contracts.Integrations;

public sealed record WebhookSubscriptionSummaryDto(
    Guid Id,
    string Name,
    string EndpointUrl,
    bool IsActive,
    IReadOnlyList<string> EventTypes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record WebhookSubscriptionDetailsDto(
    Guid Id,
    string Name,
    string EndpointUrl,
    bool IsActive,
    IReadOnlyList<string> EventTypes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record WebhookDeliverySummaryDto(
    Guid Id,
    Guid WebhookSubscriptionId,
    Guid EventId,
    string EventType,
    string Status,
    int AttemptCount,
    DateTime? LastAttemptAtUtc,
    DateTime? NextAttemptAtUtc,
    int? ResponseCode,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record WebhookDeliveryDetailsDto(
    Guid Id,
    Guid WebhookSubscriptionId,
    Guid EventId,
    string EventType,
    string Status,
    string PayloadJson,
    int AttemptCount,
    DateTime? LastAttemptAtUtc,
    DateTime? NextAttemptAtUtc,
    int? ResponseCode,
    string? ResponseBody,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
