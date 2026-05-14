using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Orders;
using Platform.Application.Orders.Commands;
using Platform.Application.Orders.Queries;
using Platform.Application.Security;
using Platform.Contracts.Common;
using Platform.Contracts.Orders;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("api/admin/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly IOrderAdminApplicationService _orderService;

    public OrdersController(IOrderAdminApplicationService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrderSummaryDto>>> ListAsync(
        [FromQuery] string? status,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? fulfillmentStatus,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? marketId,
        [FromQuery] DateTime? placedFromUtc,
        [FromQuery] DateTime? placedToUtc,
        [FromQuery] string? search,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.ListAsync(
            new ListOrdersQuery(
                status,
                paymentStatus,
                fulfillmentStatus,
                customerId,
                companyId,
                marketId,
                placedFromUtc,
                placedToUtc,
                search,
                page,
                pageSize,
                sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminOrderById")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(new GetOrderByIdQuery(id), cancellationToken);
        return order is null ? NotFoundProblem("Order", id) : Ok(order);
    }

    [HttpGet("{id:guid}/status-history")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusHistoryDto>>> GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var history = await _orderService.GetStatusHistoryAsync(new GetOrderStatusHistoryQuery(id), cancellationToken);
        return history is null ? NotFoundProblem("Order", id) : Ok(history);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDetailsDto>> CreateAsync([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var created = await _orderService.CreateAsync(
            new CreateOrderCommand(
                request.CartId,
                request.CartRowVersion,
                request.CustomerId,
                request.CompanyId,
                request.MarketId,
                request.CurrencyCode,
                request.CultureCode,
                request.Email,
                request.Lines.Select(x => new CreateOrderLineItem(x.VariantId, x.Quantity, x.Comment)).ToList(),
                request.Addresses.Select(x => new CreateOrderAddressItem(
                    x.Type,
                    x.FirstName,
                    x.LastName,
                    x.CompanyName,
                    x.Line1,
                    x.Line2,
                    x.PostalCode,
                    x.City,
                    x.Region,
                    x.CountryCode,
                    x.Email,
                    x.Phone)).ToList()),
            User.Identity?.Name ?? "system",
            cancellationToken);

        return CreatedAtRoute("GetAdminOrderById", new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(OrderStatusHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderStatusHistoryDto>> ChangeStatusAsync(Guid id, [FromBody] ChangeOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var history = await _orderService.ChangeStatusAsync(
            new ChangeOrderStatusCommand(id, request.ToStatus, request.Comment, request.RowVersion),
            User.Identity?.Name ?? "system",
            cancellationToken);

        return history is null ? NotFoundProblem("Order", id) : Ok(history);
    }

    [HttpPost("{id:guid}/payment-transactions")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(PaymentTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentTransactionDto>> AddPaymentTransactionAsync(
        Guid id,
        [FromBody] AddPaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await _orderService.AddPaymentTransactionAsync(
            new AddPaymentTransactionCommand(
                id,
                request.Provider,
                request.ProviderReference,
                request.Type,
                request.Status,
                request.Amount,
                request.CurrencyCode,
                request.RequestedAtUtc,
                request.CompletedAtUtc,
                request.RowVersion),
            cancellationToken);

        return transaction is null ? NotFoundProblem("Order", id) : Ok(transaction);
    }
}
