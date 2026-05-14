using Platform.Application.Cart.Commands;
using Platform.Application.Cart.Queries;
using Platform.Contracts.Cart;
using Platform.Contracts.Common;

namespace Platform.Application.Cart;

public interface ICartAdminApplicationService
{
    Task<PagedResponse<CartSummaryDto>> ListAsync(ListCartsQuery query, CancellationToken cancellationToken);
    Task<CartDetailsDto?> GetByIdAsync(GetCartByIdQuery query, CancellationToken cancellationToken);
    Task<CartDetailsDto?> RepriceAsync(RepriceCartCommand command, CancellationToken cancellationToken);
    Task<CartDetailsDto?> ExpireAsync(ExpireCartCommand command, CancellationToken cancellationToken);
}
