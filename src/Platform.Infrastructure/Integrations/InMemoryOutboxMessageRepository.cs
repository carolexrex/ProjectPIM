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

    public Task<IReadOnlyList<OutboxMessage>> ListRunnableByEventTypeAsync(
        string eventType,
        int maxMessages,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxMessages <= 0)
        {
            return Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        }

        IReadOnlyList<OutboxMessage> messages = _store.OutboxMessages.Values
            .Where(x =>
                !x.IsPublished
                && !x.IsProcessingAbandoned
                && string.Equals(x.EventType, eventType, StringComparison.OrdinalIgnoreCase)
                && (!x.NextProcessingAttemptAtUtc.HasValue || x.NextProcessingAttemptAtUtc <= nowUtc))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Take(maxMessages)
            .ToList();

        return Task.FromResult(messages);
    }

    public Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.OutboxMessages[outboxMessage.Id] = outboxMessage;
        return Task.CompletedTask;
    }
}
