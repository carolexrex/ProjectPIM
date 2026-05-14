using Microsoft.EntityFrameworkCore;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Integrations;

public sealed class EfWebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfWebhookSubscriptionRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookSubscriptionListResult> ListAsync(ListWebhookSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Name.Contains(query.Search)
                || x.EndpointUrl.Contains(query.Search))
            .Where(x => query.IsActive == null || x.IsActive == query.IsActive)
            .Where(x => string.IsNullOrWhiteSpace(query.EventType) || EF.Functions.ILike(x.EventTypesJson, $"%\"{query.EventType}\"%"));

        var total = await filtered.CountAsync(cancellationToken);
        var items = await ApplySorting(filtered, query.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new WebhookSubscriptionListResult(items, total);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> ListActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookSubscriptions
            .Where(x => x.IsActive && EF.Functions.ILike(x.EventTypesJson, $"%\"{eventType}\"%"))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<WebhookSubscription?> GetByIdAsync(Guid webhookSubscriptionId, CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookSubscriptions.FirstOrDefaultAsync(x => x.Id == webhookSubscriptionId, cancellationToken);
    }

    public async Task AddAsync(WebhookSubscription webhookSubscription, CancellationToken cancellationToken)
    {
        await _dbContext.WebhookSubscriptions.AddAsync(webhookSubscription, cancellationToken);
    }

    private static IQueryable<WebhookSubscription> ApplySorting(IQueryable<WebhookSubscription> subscriptions, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "name" => subscriptions.OrderBy(x => x.Name).ThenBy(x => x.Id),
            "-updatedatutc" => subscriptions.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id),
            "updatedatutc" => subscriptions.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            _ => subscriptions.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
