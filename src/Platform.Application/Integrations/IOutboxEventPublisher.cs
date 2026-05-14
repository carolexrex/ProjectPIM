namespace Platform.Application.Integrations;

public interface IOutboxEventPublisher
{
    Task EnqueueAsync(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        CancellationToken cancellationToken);
}
