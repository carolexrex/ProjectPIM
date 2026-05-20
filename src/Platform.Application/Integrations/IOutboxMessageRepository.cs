using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IOutboxMessageRepository
{
    Task<OutboxMessage?> GetNextUnpublishedByEventTypesAsync(IReadOnlySet<string> eventTypes, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> ListRunnableByEventTypeAsync(string eventType, int maxMessages, DateTime nowUtc, CancellationToken cancellationToken);
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
}
