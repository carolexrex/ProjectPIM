using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IOutboxMessageRepository
{
    Task<OutboxMessage?> GetNextUnpublishedByEventTypeAsync(string eventType, CancellationToken cancellationToken);
    Task<OutboxMessage?> GetNextUnpublishedByEventTypesAsync(IReadOnlySet<string> eventTypes, CancellationToken cancellationToken);
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
}
