using Platform.Application.Integrations;
using Platform.Contracts.Integrations;
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

    public Task<OutboxMessageListResult> ListByEventTypeAsync(
        string eventType,
        string? status,
        int page,
        int pageSize,
        string? sort,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 50 : pageSize;

        var filtered = ApplySorting(
                ApplyStatusFilter(
                    _store.OutboxMessages.Values
                        .Where(x => string.Equals(x.EventType, eventType, StringComparison.OrdinalIgnoreCase)),
                    status,
                    nowUtc),
                sort)
            .ToList();

        return Task.FromResult(new OutboxMessageListResult(
            filtered.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToList(),
            filtered.Count));
    }

    public Task<OutboxMessage?> GetByIdAsync(Guid outboxMessageId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.OutboxMessages.TryGetValue(outboxMessageId, out var message) ? message : null);
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

    private static IEnumerable<OutboxMessage> ApplyStatusFilter(
        IEnumerable<OutboxMessage> messages,
        string? status,
        DateTime nowUtc)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "open" => messages.Where(x => !x.IsPublished),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Pending, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x =>
                    !x.IsPublished
                    && !x.IsProcessingAbandoned
                    && (!x.NextProcessingAttemptAtUtc.HasValue || x.NextProcessingAttemptAtUtc <= nowUtc)),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Delayed, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x =>
                    !x.IsPublished
                    && !x.IsProcessingAbandoned
                    && x.NextProcessingAttemptAtUtc.HasValue
                    && x.NextProcessingAttemptAtUtc > nowUtc),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Abandoned, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x => !x.IsPublished && x.IsProcessingAbandoned),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Published, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x => x.IsPublished),
            _ => messages
        };
    }

    private static IOrderedEnumerable<OutboxMessage> ApplySorting(IEnumerable<OutboxMessage> messages, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "updatedatutc" => messages.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            "-updatedatutc" => messages.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id),
            "-occurredatutc" => messages.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id),
            "attempts" => messages.OrderBy(x => x.ProcessingAttemptCount).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),
            "-attempts" => messages.OrderByDescending(x => x.ProcessingAttemptCount).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id),
            _ => messages.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
        };
    }
}
