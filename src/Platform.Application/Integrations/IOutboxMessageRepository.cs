using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IOutboxMessageRepository
{
    Task<OutboxMessage?> GetNextUnpublishedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> ListUnpublishedAsync(int maxMessages, CancellationToken cancellationToken);
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
}
