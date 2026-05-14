using Microsoft.EntityFrameworkCore;
using Platform.Application.Orders;
using Platform.Application.Orders.Queries;
using Platform.Domain.Orders;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Orders;

public sealed class EfOrderRepository : IOrderRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfOrderRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderListResult> ListAsync(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Orders
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => string.IsNullOrWhiteSpace(query.PaymentStatus) || x.PaymentStatus == query.PaymentStatus)
            .Where(x => string.IsNullOrWhiteSpace(query.FulfillmentStatus) || x.FulfillmentStatus == query.FulfillmentStatus)
            .Where(x => !query.CustomerId.HasValue || x.CustomerId == query.CustomerId.Value)
            .Where(x => !query.CompanyId.HasValue || x.CompanyId == query.CompanyId.Value)
            .Where(x => !query.MarketId.HasValue || x.MarketId == query.MarketId.Value)
            .Where(x => !query.PlacedFromUtc.HasValue || x.PlacedAtUtc >= query.PlacedFromUtc.Value)
            .Where(x => !query.PlacedToUtc.HasValue || x.PlacedAtUtc <= query.PlacedToUtc.Value)
            .Where(x => string.IsNullOrWhiteSpace(query.Search) || x.OrderNumber.Contains(query.Search) || x.Email.Contains(query.Search));

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Lines)
            .Include(x => x.Addresses)
            .Include(x => x.StatusHistory)
            .Include(x => x.PaymentTransactions)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new OrderListResult(items, total);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .Include(x => x.Lines)
            .Include(x => x.Addresses)
            .Include(x => x.StatusHistory)
            .Include(x => x.PaymentTransactions)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetBySourceCartIdAsync(Guid sourceCartId, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .Include(x => x.Lines)
            .Include(x => x.Addresses)
            .Include(x => x.StatusHistory)
            .Include(x => x.PaymentTransactions)
            .FirstOrDefaultAsync(x => x.SourceCartId == sourceCartId, cancellationToken);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .Include(x => x.Lines)
            .Include(x => x.Addresses)
            .Include(x => x.StatusHistory)
            .Include(x => x.PaymentTransactions)
            .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    private static IQueryable<Order> ApplySorting(IQueryable<Order> orders, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-placedatutc" => orders.OrderByDescending(x => x.PlacedAtUtc).ThenBy(x => x.OrderNumber),
            "placedatutc" => orders.OrderBy(x => x.PlacedAtUtc).ThenBy(x => x.OrderNumber),
            "-grandtotal" => orders.OrderByDescending(x => x.GrandTotal).ThenBy(x => x.OrderNumber),
            "grandtotal" => orders.OrderBy(x => x.GrandTotal).ThenBy(x => x.OrderNumber),
            _ => orders.OrderByDescending(x => x.PlacedAtUtc).ThenBy(x => x.OrderNumber)
        };
    }
}
