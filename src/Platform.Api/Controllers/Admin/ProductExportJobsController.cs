using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Security;
using Platform.Contracts.Integrations;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogWrite)]
[Route("api/admin/products/export-jobs")]
public sealed class ProductExportJobsController : ControllerBase
{
    private readonly IIntegrationJobAdminApplicationService _integrationJobService;

    public ProductExportJobsController(IIntegrationJobAdminApplicationService integrationJobService)
    {
        _integrationJobService = integrationJobService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(IntegrationJobDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<IntegrationJobDetailsDto>> CreateAsync(
        [FromBody] CreateProductExportJobRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _integrationJobService.CreateProductExportAsync(
            new CreateProductExportJobCommand(
                request.Search,
                request.Status,
                request.ProductStatusCode,
                request.BrandId,
                request.HasVariants),
            cancellationToken);

        return CreatedAtRoute("GetIntegrationJobById", new { id = created.Id }, created);
    }
}
