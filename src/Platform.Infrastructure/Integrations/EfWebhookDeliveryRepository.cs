using Microsoft.EntityFrameworkCore;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Queries;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Integrations;

public sealed class EfWebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfWebhookDeliveryRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookDeliveryListResult> ListAsync(ListWebhookDeliveriesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _dbContext.WebhookDeliveries
            .AsNoTracking()
            .Where(x => query.WebhookSubscriptionId == null || x.WebhookSubscriptionId == query.WebhookSubscriptionId)
            .Where(x => string.IsNullOrWhiteSpace(query.EventType) || x.EventType == query.EventType)
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status);

        var total = await filtered.CountAsync(cancellationToken);
        var items = await ApplySorting(filtered, query.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new WebhookDeliveryListResult(items, total);
    }

    public async Task<WebhookDelivery?> GetByIdAsync(Guid webhookDeliveryId, CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookDeliveries.FirstOrDefaultAsync(x => x.Id == webhookDeliveryId, cancellationToken);
    }

    public async Task<WebhookDelivery?> GetNextRunnableAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookDeliveries
            .Where(x => x.Status == WebhookDeliveryStatuses.Pending
                || (x.Status == WebhookDeliveryStatuses.Failed && (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc.Value <= nowUtc)))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(WebhookDelivery webhookDelivery, CancellationToken cancellationToken)
    {
        await _dbContext.WebhookDeliveries.AddAsync(webhookDelivery, cancellationToken);
    }

    private static IQueryable<WebhookDelivery> ApplySorting(IQueryable<WebhookDelivery> deliveries, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "updatedatutc" => deliveries.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Id),
            "-updatedatutc" => deliveries.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id),
            _ => deliveries.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
