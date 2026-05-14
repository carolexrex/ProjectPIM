using Platform.Contracts.Orders;

namespace Platform.Backoffice.Models;

public sealed class OrderDetailsPageViewModel
{
    public OrderDetailsDto Order { get; init; } = default!;
    public OrderStatusChangeViewModel StatusForm { get; init; } = new();
    public OrderPaymentTransactionCreateViewModel PaymentForm { get; init; } = new();
}
