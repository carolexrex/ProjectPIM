using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Security;
using Platform.Contracts.Integrations;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogWrite)]
[Route("api/admin/storefront/projection-rebuild-jobs")]
public sealed class StorefrontProjectionRebuildJobsController : ControllerBase
{
    private readonly IIntegrationJobAdminApplicationService _integrationJobService;

    public StorefrontProjectionRebuildJobsController(IIntegrationJobAdminApplicationService integrationJobService)
    {
        _integrationJobService = integrationJobService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(IntegrationJobDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<IntegrationJobDetailsDto>> CreateAsync(
        [FromBody] CreateStorefrontProjectionRebuildJobRequest? request,
        CancellationToken cancellationToken)
    {
        _ = request;

        var created = await _integrationJobService.CreateStorefrontProjectionRebuildAsync(
            new CreateStorefrontProjectionRebuildJobCommand(),
            cancellationToken);

        return CreatedAtRoute("GetIntegrationJobById", new { id = created.Id }, created);
    }
}
