using Platform.Domain.Orders;

namespace Platform.Application.Orders;

public sealed record OrderListResult(IReadOnlyList<Order> Items, int Total);
