using Platform.Application.Orders;
using Platform.Application.Orders.Queries;
using Platform.Infrastructure.Catalog;
using Platform.Domain.Orders;

namespace Platform.Infrastructure.Orders;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryOrderRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<OrderListResult> ListAsync(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _store.Orders.Values
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.PaymentStatus) || string.Equals(x.PaymentStatus, query.PaymentStatus, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.FulfillmentStatus) || string.Equals(x.FulfillmentStatus, query.FulfillmentStatus, StringComparison.OrdinalIgnoreCase))
            .Where(x => !query.CustomerId.HasValue || x.CustomerId == query.CustomerId.Value)
            .Where(x => !query.CompanyId.HasValue || x.CompanyId == query.CompanyId.Value)
            .Where(x => !query.MarketId.HasValue || x.MarketId == query.MarketId.Value)
            .Where(x => !query.PlacedFromUtc.HasValue || x.PlacedAtUtc >= query.PlacedFromUtc.Value)
            .Where(x => !query.PlacedToUtc.HasValue || x.PlacedAtUtc <= query.PlacedToUtc.Value)
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.OrderNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || x.Email.Contains(query.Search, StringComparison.OrdinalIgnoreCase));

        filtered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "-placedatutc" => filtered.OrderByDescending(x => x.PlacedAtUtc).ThenBy(x => x.OrderNumber),
            "placedatutc" => filtered.OrderBy(x => x.PlacedAtUtc).ThenBy(x => x.OrderNumber),
            "-grandtotal" => filtered.OrderByDescending(x => x.GrandTotal).ThenBy(x => x.OrderNumber),
            "grandtotal" => filtered.OrderBy(x => x.GrandTotal).ThenBy(x => x.OrderNumber),
            _ => filtered.OrderByDescending(x => x.PlacedAtUtc).ThenBy(x => x.OrderNumber)
        };

        var materialized = filtered.ToList();
        return Task.FromResult(new OrderListResult(materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(), materialized.Count));
    }

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        _store.Orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<Order?> GetBySourceCartIdAsync(Guid sourceCartId, CancellationToken cancellationToken)
    {
        var order = _store.Orders.Values.FirstOrDefault(x => x.SourceCartId == sourceCartId);
        return Task.FromResult(order);
    }

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken)
    {
        var order = _store.Orders.Values.FirstOrDefault(x => string.Equals(x.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(order);
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _store.Orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
