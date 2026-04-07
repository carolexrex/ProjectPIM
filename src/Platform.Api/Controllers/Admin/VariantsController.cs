using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Variants;
using Platform.Application.Catalog.Variants.Commands;
using Platform.Application.Catalog.Variants.Queries;
using Platform.Contracts.Catalog.Variants;

namespace Platform.Api.Controllers.Admin;

[ApiController]
public sealed class VariantsController : ApiControllerBase
{
    private readonly IVariantAdminApplicationService _variantService;

    public VariantsController(IVariantAdminApplicationService variantService)
    {
        _variantService = variantService;
    }

    [HttpGet("api/admin/products/{productId:guid}/variants")]
    [ProducesResponseType(typeof(IReadOnlyList<VariantSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VariantSummaryDto>>> ListByProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var items = await _variantService.ListByProductAsync(new ListVariantsByProductQuery(productId), cancellationToken);
        return Ok(items);
    }

    [HttpGet("api/admin/variants/{id:guid}")]
    [ProducesResponseType(typeof(VariantDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _variantService.GetByIdAsync(new GetVariantByIdQuery(id), cancellationToken);
        return item is null ? NotFoundProblem("Variant", id) : Ok(item);
    }

    [HttpPost("api/admin/products/{productId:guid}/variants")]
    [ProducesResponseType(typeof(VariantDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDetailsDto>> CreateAsync(
        Guid productId,
        [FromBody] CreateVariantRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _variantService.CreateAsync(
            new CreateVariantCommand(
                productId,
                request.Sku,
                request.Ean,
                request.Mpn,
                request.Barcode,
                request.ProductStatusDefinitionId,
                request.IsDefaultVariant,
                request.Weight,
                request.Length,
                request.Width,
                request.Height,
                request.AttributeValues.Select(x => new CreateVariantAttributeValueCommand(x.ProductAttributeId, x.AttributeOptionId, x.ValueText)).ToList()),
            cancellationToken);

        return created is null
            ? NotFoundProblem("Product", productId)
            : CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("api/admin/variants/{id:guid}")]
    [ProducesResponseType(typeof(VariantDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDetailsDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateVariantRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _variantService.UpdateAsync(
            new UpdateVariantCommand(
                id,
                request.Sku,
                request.Ean,
                request.Mpn,
                request.Barcode,
                request.ProductStatusDefinitionId,
                request.IsDefaultVariant,
                request.Weight,
                request.Length,
                request.Width,
                request.Height,
                request.AttributeValues.Select(x => new CreateVariantAttributeValueCommand(x.ProductAttributeId, x.AttributeOptionId, x.ValueText)).ToList(),
                request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Variant", id) : Ok(updated);
    }

    [HttpPost("api/admin/variants/{id:guid}/status")]
    [ProducesResponseType(typeof(VariantDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDetailsDto>> AssignStatusAsync(
        Guid id,
        [FromBody] AssignVariantStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _variantService.AssignStatusAsync(
            new AssignVariantStatusCommand(id, request.ProductStatusDefinitionId, request.Comment),
            cancellationToken);

        return updated is null ? NotFoundProblem("Variant", id) : Ok(updated);
    }
}
