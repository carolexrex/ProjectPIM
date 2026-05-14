using Platform.Contracts.Orders;

namespace Platform.Backoffice.Models;

public sealed class OrderListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? PaymentStatus { get; init; }
    public IReadOnlyList<OrderSummaryDto> Orders { get; init; } = [];
    public int Total { get; init; }
}
