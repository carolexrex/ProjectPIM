using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Security;
using Platform.Application.Security.AdminUsers;
using Platform.Application.Security.AdminUsers.Commands;
using Platform.Application.Security.AdminUsers.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Security;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.IdentityRead)]
[Route("api/admin/admin-users")]
public sealed class AdminUsersController : ApiControllerBase
{
    private readonly IAdminUserAdminApplicationService _adminUserService;

    public AdminUsersController(IAdminUserAdminApplicationService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminUserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdminUserSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminUserService.ListAsync(new ListAdminUsersQuery(search, status, page, pageSize, sort), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminUserById")]
    [ProducesResponseType(typeof(AdminUserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var adminUser = await _adminUserService.GetByIdAsync(new GetAdminUserByIdQuery(id), cancellationToken);
        return adminUser is null ? NotFoundProblem("AdminUser", id) : Ok(adminUser);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.IdentityWrite)]
    [ProducesResponseType(typeof(AdminUserDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminUserDetailsDto>> CreateAsync([FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _adminUserService.CreateAsync(
            new CreateAdminUserCommand(request.Username, request.Password, request.DisplayName, request.Status, request.Roles),
            cancellationToken);

        return CreatedAtRoute("GetAdminUserById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.IdentityWrite)]
    [ProducesResponseType(typeof(AdminUserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailsDto>> UpdateAsync(Guid id, [FromBody] UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await _adminUserService.UpdateAsync(
            new UpdateAdminUserCommand(id, request.DisplayName, request.Status, request.Roles, request.Password, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("AdminUser", id) : Ok(updated);
    }
}
