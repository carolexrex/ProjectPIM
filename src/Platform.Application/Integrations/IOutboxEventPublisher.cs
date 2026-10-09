namespace Platform.Application.Integrations;

public interface IOutboxEventPublisher
{
    Task EnqueueAsync(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues a message that carries a scheduling identity. The persistence layer rejects the
    /// insert with <see cref="Platform.Domain.Integrations.DuplicateOutboxDedupKeyException"/> when
    /// another open message already holds the same key.
    /// </summary>
    Task EnqueueDeduplicatedAsync(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        string dedupKey,
        CancellationToken cancellationToken);
}
