using Platform.Application.Cart.Queries;

namespace Platform.Application.Cart;

public interface ICartRepository
{
    Task<CartListResult> ListAsync(ListCartsQuery query, CancellationToken cancellationToken);
    Task<Platform.Domain.Cart.Cart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken);
    Task AddAsync(Platform.Domain.Cart.Cart cart, CancellationToken cancellationToken);
}
