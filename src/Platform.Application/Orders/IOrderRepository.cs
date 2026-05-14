using Platform.Application.Orders.Queries;
using Platform.Domain.Orders;

namespace Platform.Application.Orders;

public interface IOrderRepository
{
    Task<OrderListResult> ListAsync(ListOrdersQuery query, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<Order?> GetBySourceCartIdAsync(Guid sourceCartId, CancellationToken cancellationToken);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
