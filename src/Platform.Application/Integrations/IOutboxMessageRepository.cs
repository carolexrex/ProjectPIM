using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public interface IOutboxMessageRepository
{
    Task<OutboxMessage?> GetNextUnpublishedAsync(CancellationToken cancellationToken);
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
}
