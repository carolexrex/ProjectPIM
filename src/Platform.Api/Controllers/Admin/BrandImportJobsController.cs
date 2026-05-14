using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Security;
using Platform.Contracts.Integrations;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogWrite)]
[Route("api/admin/brands/import-jobs")]
public sealed class BrandImportJobsController : ControllerBase
{
    private readonly IIntegrationJobAdminApplicationService _integrationJobService;

    public BrandImportJobsController(IIntegrationJobAdminApplicationService integrationJobService)
    {
        _integrationJobService = integrationJobService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(IntegrationJobDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<IntegrationJobDetailsDto>> CreateAsync(
        [FromBody] CreateBrandImportJobRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _integrationJobService.CreateBrandImportAsync(
            new CreateBrandImportJobCommand(
                request.Brands.Select(
                    brand => new BrandImportJobItemInput(
                        brand.Code,
                        brand.WebsiteUrl,
                        brand.LogoMediaAssetId,
                        brand.SortOrder,
                        brand.Translations.Select(
                            translation => new BrandImportJobTranslationInput(
                                translation.CultureCode,
                                translation.Name,
                                translation.Slug,
                                translation.Description))
                            .ToList()))
                    .ToList()),
            cancellationToken);

        return CreatedAtRoute("GetIntegrationJobById", new { id = created.Id }, created);
    }
}
