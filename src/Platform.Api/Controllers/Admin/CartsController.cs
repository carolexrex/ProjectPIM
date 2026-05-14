using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Cart;
using Platform.Application.Cart.Commands;
using Platform.Application.Cart.Queries;
using Platform.Application.Security;
using Platform.Contracts.Cart;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("api/admin/carts")]
public sealed class CartsController : ApiControllerBase
{
    private readonly ICartAdminApplicationService _cartService;

    public CartsController(ICartAdminApplicationService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CartSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CartSummaryDto>>> ListAsync(
        [FromQuery] string? status,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? marketId,
        [FromQuery] DateTime? createdFromUtc,
        [FromQuery] DateTime? createdToUtc,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cartService.ListAsync(
            new ListCartsQuery(status, customerId, companyId, marketId, createdFromUtc, createdToUtc, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminCartById")]
    [ProducesResponseType(typeof(CartDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetByIdAsync(new GetCartByIdQuery(id), cancellationToken);
        return cart is null ? NotFoundProblem("Cart", id) : Ok(cart);
    }

    [HttpPost("{id:guid}/reprice")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CartDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDetailsDto>> RepriceAsync(Guid id, [FromBody] RepriceCartRequest request, CancellationToken cancellationToken)
    {
        var cart = await _cartService.RepriceAsync(new RepriceCartCommand(id, request.RowVersion), cancellationToken);
        return cart is null ? NotFoundProblem("Cart", id) : Ok(cart);
    }

    [HttpPost("{id:guid}/expire")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CartDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDetailsDto>> ExpireAsync(Guid id, [FromBody] ExpireCartRequest request, CancellationToken cancellationToken)
    {
        var cart = await _cartService.ExpireAsync(new ExpireCartCommand(id, request.RowVersion), cancellationToken);
        return cart is null ? NotFoundProblem("Cart", id) : Ok(cart);
    }
}
