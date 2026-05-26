namespace Platform.Contracts.Integrations;

public static class StorefrontProjectionRefreshMessageStatuses
{
    public const string Pending = "Pending";
    public const string Delayed = "Delayed";
    public const string Abandoned = "Abandoned";
    public const string Published = "Published";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Delayed,
        Abandoned,
        Published
    };
}

public sealed record StorefrontProjectionRefreshMessageSummaryDto(
    Guid Id,
    string EventType,
    string AggregateType,
    Guid AggregateId,
    string Status,
    int ProcessingAttemptCount,
    string? LastProcessingError,
    DateTime? NextProcessingAttemptAtUtc,
    DateTime? ProcessingAbandonedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime OccurredAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record StorefrontProjectionRefreshMessageDetailsDto(
    Guid Id,
    string EventType,
    string AggregateType,
    Guid AggregateId,
    string Status,
    string PayloadJson,
    int ProcessingAttemptCount,
    string? LastProcessingError,
    DateTime? NextProcessingAttemptAtUtc,
    DateTime? ProcessingAbandonedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime OccurredAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
