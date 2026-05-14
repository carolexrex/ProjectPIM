using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Companies;
using Platform.Application.Companies.Commands;
using Platform.Application.Companies.Queries;
using Platform.Application.Security;
using Platform.Contracts.Common;
using Platform.Contracts.Companies;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CustomerRead)]
[Route("api/admin/companies")]
public sealed class CompaniesController : ApiControllerBase
{
    private readonly ICompanyAdminApplicationService _companyService;

    public CompaniesController(ICompanyAdminApplicationService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CompanySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CompanySummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? defaultMarketId,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _companyService.ListAsync(
            new ListCompaniesQuery(search, status, defaultMarketId, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminCompanyById")]
    [ProducesResponseType(typeof(CompanyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetByIdAsync(new GetCompanyByIdQuery(id), cancellationToken);
        return company is null ? NotFoundProblem("Company", id) : Ok(company);
    }

    [HttpGet("{id:guid}/memberships")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyMembershipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CompanyMembershipDto>>> ListMembershipsAsync(Guid id, CancellationToken cancellationToken)
    {
        var memberships = await _companyService.ListMembershipsAsync(new ListCompanyMembershipsQuery(id), cancellationToken);
        return memberships is null ? NotFoundProblem("Company", id) : Ok(memberships);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CompanyDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CompanyDetailsDto>> CreateAsync([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var created = await _companyService.CreateAsync(
            new CreateCompanyCommand(
                request.ExternalId,
                request.Code,
                request.Name,
                request.LegalName,
                request.OrganizationNumber,
                request.VatNumber,
                request.Email,
                request.Phone,
                request.DefaultMarketId,
                request.DefaultCurrency,
                request.Status),
            cancellationToken);

        return CreatedAtRoute("GetAdminCompanyById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CompanyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyDetailsDto>> UpdateAsync(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var updated = await _companyService.UpdateAsync(
            new UpdateCompanyCommand(
                id,
                request.ExternalId,
                request.Code,
                request.Name,
                request.LegalName,
                request.OrganizationNumber,
                request.VatNumber,
                request.Email,
                request.Phone,
                request.DefaultMarketId,
                request.DefaultCurrency,
                request.Status,
                request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Company", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/addresses")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CompanyAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyAddressDto>> AddAddressAsync(Guid id, [FromBody] AddCompanyAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _companyService.AddAddressAsync(
            new AddCompanyAddressCommand(
                id,
                request.Type,
                request.Attention,
                request.Line1,
                request.Line2,
                request.PostalCode,
                request.City,
                request.Region,
                request.CountryCode,
                request.Email,
                request.Phone,
                request.IsDefault),
            cancellationToken);

        return address is null ? NotFoundProblem("Company", id) : Ok(address);
    }

    [HttpPost("{id:guid}/memberships")]
    [Authorize(Policy = AdminPolicies.CustomerWrite)]
    [ProducesResponseType(typeof(CompanyMembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyMembershipDto>> CreateMembershipAsync(Guid id, [FromBody] CreateCompanyMembershipRequest request, CancellationToken cancellationToken)
    {
        var membership = await _companyService.CreateMembershipAsync(
            new CreateCompanyMembershipCommand(
                id,
                request.CustomerId,
                request.Role,
                request.Status,
                request.IsDefaultCompany,
                request.CanPlaceOrders,
                request.CanApproveOrders,
                request.CanManageUsers,
                request.ValidFromUtc,
                request.ValidToUtc),
            cancellationToken);

        return membership is null ? NotFoundProblem("Company", id) : Ok(membership);
    }
}
