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

    public Task<OutboxMessage?> GetNextUnpublishedByEventTypeAsync(string eventType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = _store.OutboxMessages.Values
            .Where(x => !x.IsPublished && string.Equals(x.EventType, eventType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        return Task.FromResult(message);
    }

    public Task<OutboxMessage?> GetNextUnpublishedByEventTypesAsync(IReadOnlySet<string> eventTypes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (eventTypes.Count == 0)
        {
            return Task.FromResult<OutboxMessage?>(null);
        }

        var message = _store.OutboxMessages.Values
            .Where(x => !x.IsPublished && eventTypes.Contains(x.EventType))
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
