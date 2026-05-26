using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IOutboxMessageRepository
{
    Task<OutboxMessageListResult> ListByEventTypeAsync(
        string eventType,
        string? status,
        int page,
        int pageSize,
        string? sort,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<OutboxMessage?> GetByIdAsync(Guid outboxMessageId, CancellationToken cancellationToken);
    Task<OutboxMessage?> GetNextUnpublishedByEventTypesAsync(IReadOnlySet<string> eventTypes, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> ListRunnableByEventTypeAsync(string eventType, int maxMessages, DateTime nowUtc, CancellationToken cancellationToken);
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
}
