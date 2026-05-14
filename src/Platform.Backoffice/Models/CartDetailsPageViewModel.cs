using Platform.Contracts.Cart;

namespace Platform.Backoffice.Models;

public sealed class CartDetailsPageViewModel
{
    public CartDetailsDto Cart { get; init; } = default!;
    public CartActionViewModel RepriceForm { get; init; } = new();
    public CartActionViewModel ExpireForm { get; init; } = new();
    public OrderFromCartCreateViewModel CreateOrderForm { get; init; } = new();
}
