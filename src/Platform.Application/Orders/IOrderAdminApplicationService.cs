using Platform.Application.Orders.Commands;
using Platform.Application.Orders.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Orders;

namespace Platform.Application.Orders;

public interface IOrderAdminApplicationService
{
    Task<PagedResponse<OrderSummaryDto>> ListAsync(ListOrdersQuery query, CancellationToken cancellationToken);
    Task<OrderDetailsDto?> GetByIdAsync(GetOrderByIdQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderStatusHistoryDto>?> GetStatusHistoryAsync(GetOrderStatusHistoryQuery query, CancellationToken cancellationToken);
    Task<OrderDetailsDto> CreateAsync(CreateOrderCommand command, string requestedBy, CancellationToken cancellationToken);
    Task<OrderStatusHistoryDto?> ChangeStatusAsync(ChangeOrderStatusCommand command, string changedBy, CancellationToken cancellationToken);
    Task<PaymentTransactionDto?> AddPaymentTransactionAsync(AddPaymentTransactionCommand command, CancellationToken cancellationToken);
}
