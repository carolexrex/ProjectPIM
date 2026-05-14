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

    public async Task EnqueueAsync(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            eventType,
            aggregateType,
            aggregateId,
            payloadJson,
            DateTime.UtcNow);

        await _outboxMessageRepository.AddAsync(message, cancellationToken);
    }
}
