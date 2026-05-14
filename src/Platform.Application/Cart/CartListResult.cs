using Platform.Domain.Cart;

namespace Platform.Application.Cart;

public sealed record CartListResult(IReadOnlyList<Platform.Domain.Cart.Cart> Items, int Total);
