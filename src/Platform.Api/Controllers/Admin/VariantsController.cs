using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Inventory.Queries;
using Platform.Application.Catalog.Variants;
using Platform.Application.Catalog.Variants.Commands;
using Platform.Application.Catalog.Variants.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Inventory;
using Platform.Contracts.Catalog.Variants;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
public sealed class VariantsController : ApiControllerBase
{
    private readonly IVariantAdminApplicationService _variantService;
    private readonly IInventoryAdminApplicationService _inventoryService;

    public VariantsController(IVariantAdminApplicationService variantService, IInventoryAdminApplicationService inventoryService)
    {
        _variantService = variantService;
        _inventoryService = inventoryService;
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

    [HttpGet("api/admin/variants/lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<VariantLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VariantLookupDto>>> LookupAsync(
        [FromQuery] string? search,
        [FromQuery] string? status = "Active",
        [FromQuery] Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _variantService.ListLookupsAsync(
            new ListVariantLookupsQuery(search, status, productId),
            cancellationToken);
        return Ok(items);
    }

    [HttpGet("api/admin/variants/{id:guid}", Name = "GetAdminVariantById")]
    [ProducesResponseType(typeof(VariantDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _variantService.GetByIdAsync(new GetVariantByIdQuery(id), cancellationToken);
        return item is null ? NotFoundProblem("Variant", id) : Ok(item);
    }

    [HttpGet("api/admin/variants/{id:guid}/inventory")]
    [Authorize(Policy = AdminPolicies.InventoryRead)]
    [ProducesResponseType(typeof(VariantInventorySnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantInventorySnapshotDto>> GetInventorySnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        var snapshot = await _inventoryService.GetVariantInventorySnapshotAsync(new GetVariantInventorySnapshotQuery(id), cancellationToken);
        return snapshot is null ? NotFoundProblem("Variant", id) : Ok(snapshot);
    }

    [HttpPost("api/admin/products/{productId:guid}/variants")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
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
            : CreatedAtRoute("GetAdminVariantById", new { id = created.Id }, created);
    }

    [HttpPut("api/admin/variants/{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
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
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
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

    [HttpPost("api/admin/variants/{id:guid}/media")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(VariantDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDetailsDto>> UpsertMediaAsync(
        Guid id,
        [FromBody] UpsertVariantMediaRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _variantService.UpsertMediaAsync(
            new UpsertVariantMediaCommand(id, request.MediaAssetId, request.Type, request.SortOrder, request.IsPrimary, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Variant", id) : Ok(updated);
    }

    [HttpPost("api/admin/variants/{id:guid}/media/{variantMediaId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(VariantDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDetailsDto>> RemoveMediaAsync(
        Guid id,
        Guid variantMediaId,
        [FromBody] RemoveVariantMediaRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _variantService.RemoveMediaAsync(
            new RemoveVariantMediaCommand(id, variantMediaId, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Variant", id) : Ok(updated);
    }
}
