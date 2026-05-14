using Platform.Application.Integrations;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Integrations;

public sealed class InMemoryOutboxMessageRepository : IOutboxMessageRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryOutboxMessageRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<OutboxMessage?> GetNextUnpublishedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = _store.OutboxMessages.Values
            .Where(x => !x.IsPublished)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        return Task.FromResult(message);
    }

    public Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.OutboxMessages[outboxMessage.Id] = outboxMessage;
        return Task.CompletedTask;
    }
}
