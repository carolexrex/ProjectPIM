using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Security;
using Platform.Contracts.Integrations;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogWrite)]
[Route("api/admin/products/import-jobs")]
public sealed class ProductImportJobsController : ControllerBase
{
    private readonly IIntegrationJobAdminApplicationService _integrationJobService;

    public ProductImportJobsController(IIntegrationJobAdminApplicationService integrationJobService)
    {
        _integrationJobService = integrationJobService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(IntegrationJobDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<IntegrationJobDetailsDto>> CreateAsync(
        [FromBody] CreateProductImportJobRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _integrationJobService.CreateProductImportAsync(
            new CreateProductImportJobCommand(
                request.Products.Select(
                    product => new ProductImportJobItemInput(
                        product.ProductType,
                        product.ProductNumber,
                        product.Slug,
                        product.BrandCode,
                        product.ProductStatusCode,
                        product.TaxCategoryCode,
                        product.UnitOfMeasure,
                        product.HasVariants,
                        product.Weight,
                        product.Length,
                        product.Width,
                        product.Height,
                        product.CategoryCodes.ToList(),
                        product.AttributeValues.Select(
                            attributeValue => new ProductImportJobAttributeValueInput(
                                attributeValue.ProductAttributeCode,
                                attributeValue.AttributeOptionCode,
                                attributeValue.ValueText))
                            .ToList(),
                        product.Translations.Select(
                            translation => new ProductImportJobTranslationInput(
                                translation.CultureCode,
                                translation.Name,
                                translation.ShortDescription,
                                translation.LongDescription,
                                translation.SeoTitle,
                                translation.SeoDescription))
                            .ToList()))
                    .ToList()),
            cancellationToken);

        return CreatedAtRoute("GetIntegrationJobById", new { id = created.Id }, created);
    }
}
