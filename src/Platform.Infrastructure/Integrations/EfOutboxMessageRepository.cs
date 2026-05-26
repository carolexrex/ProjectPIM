using Microsoft.EntityFrameworkCore;
using Platform.Application.Integrations;
using Platform.Contracts.Integrations;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Integrations;

public sealed class EfOutboxMessageRepository : IOutboxMessageRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfOutboxMessageRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OutboxMessageListResult> ListByEventTypeAsync(
        string eventType,
        string? status,
        int page,
        int pageSize,
        string? sort,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 50 : pageSize;

        var filtered = ApplyStatusFilter(
            _dbContext.OutboxMessages
                .AsNoTracking()
                .Where(x => x.EventType == eventType),
            status,
            nowUtc);

        var total = await filtered.CountAsync(cancellationToken);
        var items = await ApplySorting(filtered, sort)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new OutboxMessageListResult(items, total);
    }

    public async Task<OutboxMessage?> GetByIdAsync(Guid outboxMessageId, CancellationToken cancellationToken)
    {
        return await _dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == outboxMessageId, cancellationToken);
    }

    public async Task<OutboxMessage?> GetNextUnpublishedByEventTypesAsync(IReadOnlySet<string> eventTypes, CancellationToken cancellationToken)
    {
        if (eventTypes.Count == 0)
        {
            return null;
        }

        return await _dbContext.OutboxMessages
            .Where(x => !x.PublishedAtUtc.HasValue && eventTypes.Contains(x.EventType))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListRunnableByEventTypeAsync(
        string eventType,
        int maxMessages,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (maxMessages <= 0)
        {
            return [];
        }

        return await _dbContext.OutboxMessages
            .Where(x =>
                !x.PublishedAtUtc.HasValue
                && !x.ProcessingAbandonedAtUtc.HasValue
                && x.EventType == eventType
                && (!x.NextProcessingAttemptAtUtc.HasValue || x.NextProcessingAttemptAtUtc <= nowUtc))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Take(maxMessages)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }

    private static IQueryable<OutboxMessage> ApplyStatusFilter(
        IQueryable<OutboxMessage> messages,
        string? status,
        DateTime nowUtc)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "open" => messages.Where(x => !x.PublishedAtUtc.HasValue),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Pending, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x =>
                    !x.PublishedAtUtc.HasValue
                    && !x.ProcessingAbandonedAtUtc.HasValue
                    && (!x.NextProcessingAttemptAtUtc.HasValue || x.NextProcessingAttemptAtUtc <= nowUtc)),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Delayed, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x =>
                    !x.PublishedAtUtc.HasValue
                    && !x.ProcessingAbandonedAtUtc.HasValue
                    && x.NextProcessingAttemptAtUtc.HasValue
                    && x.NextProcessingAttemptAtUtc > nowUtc),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Abandoned, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x => !x.PublishedAtUtc.HasValue && x.ProcessingAbandonedAtUtc.HasValue),
            var value when string.Equals(value, StorefrontProjectionRefreshMessageStatuses.Published, StringComparison.OrdinalIgnoreCase)
                => messages.Where(x => x.PublishedAtUtc.HasValue),
            _ => messages
        };
    }

    private static IQueryable<OutboxMessage> ApplySorting(IQueryable<OutboxMessage> messages, string? sort)
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
