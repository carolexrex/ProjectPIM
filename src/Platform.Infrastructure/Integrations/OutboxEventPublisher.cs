using Platform.Application.Integrations;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class OutboxEventPublisher : IOutboxEventPublisher
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    public OutboxEventPublisher(IOutboxMessageRepository outboxMessageRepository)
    {
        _outboxMessageRepository = outboxMessageRepository;
    }

    public Task EnqueueAsync(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        return AddAsync(eventType, aggregateType, aggregateId, payloadJson, null, cancellationToken);
    }

    public Task EnqueueDeduplicatedAsync(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        string dedupKey,
        CancellationToken cancellationToken)
    {
        return AddAsync(eventType, aggregateType, aggregateId, payloadJson, dedupKey, cancellationToken);
    }

    private async Task AddAsync(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        string? dedupKey,
        CancellationToken cancellationToken)
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            eventType,
            aggregateType,
            aggregateId,
            payloadJson,
            DateTime.UtcNow,
            dedupKey);

        await _outboxMessageRepository.AddAsync(message, cancellationToken);
    }
}
