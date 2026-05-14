using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Security;
using Platform.Contracts.Integrations;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogWrite)]
[Route("api/admin/brands/export-jobs")]
public sealed class BrandExportJobsController : ControllerBase
{
    private readonly IIntegrationJobAdminApplicationService _integrationJobService;

    public BrandExportJobsController(IIntegrationJobAdminApplicationService integrationJobService)
    {
        _integrationJobService = integrationJobService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(IntegrationJobDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<IntegrationJobDetailsDto>> CreateAsync(
        [FromBody] CreateBrandExportJobRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _integrationJobService.CreateBrandExportAsync(
            new CreateBrandExportJobCommand(request.Search, request.Status),
            cancellationToken);

        return CreatedAtRoute("GetIntegrationJobById", new { id = created.Id }, created);
    }
}
