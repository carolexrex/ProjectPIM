using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Products.Commands;
using Platform.Application.Catalog.Products.Queries;
using Platform.Contracts.Catalog.Products;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
public sealed class ProductsController : ApiControllerBase
{
    private readonly IProductAdminApplicationService _productService;

    public ProductsController(IProductAdminApplicationService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? productStatusCode,
        [FromQuery] Guid? brandId,
        [FromQuery] bool? hasVariants,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.ListAsync(
            new ListProductsQuery(search, status, productStatusCode, brandId, hasVariants, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByIdAsync(new GetProductByIdQuery(id), cancellationToken);
        return result is null ? NotFoundProblem("Product", id) : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDetailsDto>> CreateAsync(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(
            new CreateProductCommand(
                request.ProductType,
                request.ProductNumber,
                request.Slug,
                request.BrandId,
                request.ProductStatusDefinitionId,
                request.TaxCategoryCode,
                request.UnitOfMeasure,
                request.HasVariants,
                request.Weight,
                request.Length,
                request.Width,
                request.Height),
            cancellationToken);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _productService.UpdateAsync(
            new UpdateProductCommand(
                id,
                request.ProductType,
                request.Slug,
                request.BrandId,
                request.ProductStatusDefinitionId,
                request.TaxCategoryCode,
                request.UnitOfMeasure,
                request.Weight,
                request.Length,
                request.Width,
                request.Height,
                request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Product", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _productService.ArchiveAsync(new ArchiveProductCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("Product", id) : Ok(archived);
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> AssignStatusAsync(
        Guid id,
        [FromBody] AssignProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _productService.AssignStatusAsync(
            new AssignProductStatusCommand(id, request.ProductStatusDefinitionId, request.Comment),
            cancellationToken);

        return updated is null ? NotFoundProblem("Product", id) : Ok(updated);
    }

    [HttpPut("{id:guid}/translations/{cultureCode}")]
    [ProducesResponseType(typeof(ProductTranslationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductTranslationDto>> UpsertTranslationAsync(
        Guid id,
        [FromRoute][StringLength(16, MinimumLength = 2)] string cultureCode,
        [FromBody] UpsertProductTranslationRequest request,
        CancellationToken cancellationToken)
    {
        var translation = await _productService.UpsertTranslationAsync(
            new UpsertProductTranslationCommand(
                id,
                cultureCode,
                request.Name,
                request.ShortDescription,
                request.LongDescription,
                request.SeoTitle,
                request.SeoDescription),
            cancellationToken);

        return translation is null ? NotFoundProblem("Product", id) : Ok(translation);
    }
}
