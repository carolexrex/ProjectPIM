using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Customers;
using Platform.Application.Customers.Commands;
using Platform.Application.Customers.Queries;
using Platform.Application.Security;
using Platform.Contracts.Common;
using Platform.Contracts.Customers;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("api/admin/customers")]
public sealed class CustomersController : ApiControllerBase
{
    private readonly ICustomerAdminApplicationService _customerService;

    public CustomersController(ICustomerAdminApplicationService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CustomerSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CustomerSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] bool? isGuest,
        [FromQuery] Guid? defaultMarketId,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.ListAsync(
            new ListCustomersQuery(search, status, isGuest, defaultMarketId, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminCustomerById")]
    [ProducesResponseType(typeof(CustomerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(new GetCustomerByIdQuery(id), cancellationToken);
        return customer is null ? NotFoundProblem("Customer", id) : Ok(customer);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CustomerDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDetailsDto>> CreateAsync([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var created = await _customerService.CreateAsync(
            new CreateCustomerCommand(
                request.ExternalId,
                request.UserId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.PreferredCulture,
                request.DefaultMarketId,
                request.Status,
                request.IsGuest),
            cancellationToken);

        return CreatedAtRoute("GetAdminCustomerById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CustomerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailsDto>> UpdateAsync(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var updated = await _customerService.UpdateAsync(
            new UpdateCustomerCommand(
                id,
                request.ExternalId,
                request.UserId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.PreferredCulture,
                request.DefaultMarketId,
                request.Status,
                request.IsGuest,
                request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Customer", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/addresses")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CustomerAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerAddressDto>> AddAddressAsync(Guid id, [FromBody] AddCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _customerService.AddAddressAsync(
            new AddCustomerAddressCommand(
                id,
                request.Type,
                request.Attention,
                request.FirstName,
                request.LastName,
                request.CompanyName,
                request.Line1,
                request.Line2,
                request.PostalCode,
                request.City,
                request.Region,
                request.CountryCode,
                request.Phone,
                request.Email,
                request.IsDefault),
            cancellationToken);

        return address is null ? NotFoundProblem("Customer", id) : Ok(address);
    }
}
