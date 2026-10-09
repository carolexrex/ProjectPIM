using Platform.Domain.Common;

namespace Platform.Domain.Integrations;

public sealed class OutboxMessage
{
    private const int MaxProcessingErrorLength = 2048;
    private const int MaxDedupKeyLength = 128;

    private OutboxMessage()
    {
        Id = Guid.Empty;
        EventType = string.Empty;
        AggregateType = string.Empty;
        PayloadJson = string.Empty;
        RowVersion = string.Empty;
    }

    public OutboxMessage(
        Guid id,
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        DateTime occurredAtUtc,
        string? dedupKey = null)
    {
        Id = id;
        EventType = NormalizeRequired(eventType);
        AggregateType = NormalizeRequired(aggregateType);
        AggregateId = aggregateId;
        PayloadJson = NormalizeRequired(payloadJson);
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = occurredAtUtc;
        UpdatedAtUtc = occurredAtUtc;
        DedupKey = NormalizeDedupKey(dedupKey);
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; }
    public string AggregateType { get; private set; }
    public Guid AggregateId { get; private set; }
    public string PayloadJson { get; private set; }

    /// <summary>
    /// Optional scheduling identity. At most one unpublished message may hold a given key, so a
    /// producer that can recompute the same unit of work — such as the time-driven storefront
    /// price-validity scan — cannot stack duplicates. The reservation is held for as long as the
    /// message is unpublished, abandonment included: an abandoned message keeps its key so failed
    /// work is never silently replaced by a fresh message, and is returned to service through the
    /// admin reset workflow (<see cref="ResetProcessingForReplay"/>) instead.
    /// </summary>
    public string? DedupKey { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public int ProcessingAttemptCount { get; private set; }
    public string? LastProcessingError { get; private set; }
    public DateTime? NextProcessingAttemptAtUtc { get; private set; }
    public DateTime? ProcessingAbandonedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }

    public bool IsPublished => PublishedAtUtc.HasValue;
    public bool IsProcessingAbandoned => ProcessingAbandonedAtUtc.HasValue;

    /// <summary>
    /// True while the message still represents runnable work: pending, delayed for retry, or being
    /// processed. Abandoned and published messages are not runnable.
    /// </summary>
    public bool IsOpen => !IsPublished && !IsProcessingAbandoned;

    /// <summary>
    /// True while the message holds its <see cref="DedupKey"/> against other producers. Only
    /// publishing releases the key; an abandoned message keeps its reservation so the failure stays
    /// visible and requires the admin reset workflow rather than being rescheduled automatically.
    /// </summary>
    public bool ReservesDedupKey => DedupKey is not null && !IsPublished;

    public void MarkPublished(string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (PublishedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outbox message is already published.");
        }

        PublishedAtUtc = DateTime.UtcNow;
        LastProcessingError = null;
        NextProcessingAttemptAtUtc = null;
        Touch();
    }

    public void MarkProcessingRetry(string error, DateTime nextAttemptAtUtc, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (PublishedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outbox message is already published.");
        }

        if (ProcessingAbandonedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outbox message processing is abandoned.");
        }

        ProcessingAttemptCount++;
        LastProcessingError = NormalizeOptional(error);
        NextProcessingAttemptAtUtc = nextAttemptAtUtc;
        Touch();
    }

    public void MarkProcessingAbandoned(string error, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (PublishedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outbox message is already published.");
        }

        ProcessingAttemptCount++;
        LastProcessingError = NormalizeOptional(error);
        NextProcessingAttemptAtUtc = null;
        ProcessingAbandonedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void ResetProcessingForReplay(string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (PublishedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The outbox message is already published.");
        }

        if (!ProcessingAbandonedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Only abandoned outbox message processing can be reset.");
        }

        ProcessingAttemptCount = 0;
        LastProcessingError = null;
        NextProcessingAttemptAtUtc = null;
        ProcessingAbandonedAtUtc = null;
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The outbox message has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= MaxProcessingErrorLength
            ? normalized
            : normalized[..MaxProcessingErrorLength];
    }

    private static string? NormalizeDedupKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxDedupKeyLength)
        {
            throw new ArgumentException($"The outbox dedup key exceeds {MaxDedupKeyLength} characters.", nameof(value));
        }

        return normalized;
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
