using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Companies;
using Platform.Application.Companies.Commands;
using Platform.Application.Security;
using Platform.Contracts.Companies;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CustomerWrite)]
[Route("api/admin/company-memberships")]
public sealed class CompanyMembershipsController : ApiControllerBase
{
    private readonly ICompanyAdminApplicationService _companyService;

    public CompanyMembershipsController(ICompanyAdminApplicationService companyService)
    {
        _companyService = companyService;
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompanyMembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyMembershipDto>> UpdateAsync(Guid id, [FromBody] UpdateCompanyMembershipRequest request, CancellationToken cancellationToken)
    {
        var updated = await _companyService.UpdateMembershipAsync(
            new UpdateCompanyMembershipCommand(
                id,
                request.Role,
                request.Status,
                request.IsDefaultCompany,
                request.CanPlaceOrders,
                request.CanApproveOrders,
                request.CanManageUsers,
                request.ValidFromUtc,
                request.ValidToUtc,
                request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("CompanyMembership", id) : Ok(updated);
    }
}
