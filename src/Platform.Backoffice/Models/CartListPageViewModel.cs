using Platform.Contracts.Cart;

namespace Platform.Backoffice.Models;

public sealed class CartListPageViewModel
{
    public string? Status { get; init; }
    public IReadOnlyList<CartSummaryDto> Carts { get; init; } = [];
    public int Total { get; init; }
}
