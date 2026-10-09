namespace Platform.Domain.Integrations;

/// <summary>
/// Raised when an outbox message is enqueued while another open message already holds the same
/// <see cref="OutboxMessage.DedupKey"/>. Producers treat this as "someone else already scheduled
/// this unit of work", not as a failure of the work itself.
/// </summary>
public sealed class DuplicateOutboxDedupKeyException : Exception
{
    public DuplicateOutboxDedupKeyException(string dedupKey)
        : base($"An open outbox message already exists for dedup key '{dedupKey}'.")
    {
        DedupKey = dedupKey;
    }

    public DuplicateOutboxDedupKeyException(string dedupKey, Exception innerException)
        : base($"An open outbox message already exists for dedup key '{dedupKey}'.", innerException)
    {
        DedupKey = dedupKey;
    }

    public string DedupKey { get; }
}
