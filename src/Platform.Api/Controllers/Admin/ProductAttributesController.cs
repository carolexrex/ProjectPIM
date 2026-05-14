using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Attributes.Commands;
using Platform.Application.Catalog.Attributes.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Attributes;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/product-attributes")]
public sealed class ProductAttributesController : ApiControllerBase
{
    private readonly IProductAttributeAdminApplicationService _attributeService;

    public ProductAttributesController(IProductAttributeAdminApplicationService attributeService)
    {
        _attributeService = attributeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductAttributeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductAttributeSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? scope,
        [FromQuery] string? dataType,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _attributeService.ListAsync(
            new ListProductAttributesQuery(search, status, scope, dataType, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("editor-definitions")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductAttributeEditorDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductAttributeEditorDefinitionDto>>> ListEditorDefinitionsAsync(
        [FromQuery][StringLength(32, MinimumLength = 3)] string scope,
        [FromQuery] string? status = "Active",
        CancellationToken cancellationToken = default)
    {
        var result = await _attributeService.ListEditorDefinitionsAsync(
            new ListProductAttributeEditorDefinitionsQuery(scope, status),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminProductAttributeById")]
    [ProducesResponseType(typeof(ProductAttributeDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductAttributeDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var attribute = await _attributeService.GetByIdAsync(new GetProductAttributeByIdQuery(id), cancellationToken);
        return attribute is null ? NotFoundProblem("ProductAttribute", id) : Ok(attribute);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ProductAttributeDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductAttributeDetailsDto>> CreateAsync(
        [FromBody] CreateProductAttributeRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _attributeService.CreateAsync(
            new CreateProductAttributeCommand(
                request.Code,
                request.Name,
                request.Scope,
                request.DataType,
                request.IsVariantDefining,
                request.IsFilterable,
                request.IsRequired,
                request.SortOrder,
                request.Options.Select(MapOption).ToList()),
            cancellationToken);

        return CreatedAtRoute("GetAdminProductAttributeById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ProductAttributeDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductAttributeDetailsDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateProductAttributeRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _attributeService.UpdateAsync(
            new UpdateProductAttributeCommand(
                id,
                request.Code,
                request.Name,
                request.Scope,
                request.DataType,
                request.IsVariantDefining,
                request.IsFilterable,
                request.IsRequired,
                request.SortOrder,
                request.RowVersion,
                request.Options.Select(MapOption).ToList()),
            cancellationToken);

        return updated is null ? NotFoundProblem("ProductAttribute", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ProductAttributeDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductAttributeDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _attributeService.ArchiveAsync(new ArchiveProductAttributeCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("ProductAttribute", id) : Ok(archived);
    }

    private static UpsertAttributeOptionCommand MapOption(AttributeOptionRequest request)
    {
        return new UpsertAttributeOptionCommand(request.Id, request.Code, request.Value, request.SortOrder);
    }
}
