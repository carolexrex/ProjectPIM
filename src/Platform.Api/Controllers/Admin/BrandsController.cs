using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Brands.Commands;
using Platform.Application.Catalog.Brands.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Brands;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/brands")]
public sealed class BrandsController : ApiControllerBase
{
    private readonly IBrandAdminApplicationService _brandService;

    public BrandsController(IBrandAdminApplicationService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BrandSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BrandSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _brandService.ListAsync(
            new ListBrandsQuery(search, status, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminBrandById")]
    [ProducesResponseType(typeof(BrandDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _brandService.GetByIdAsync(new GetBrandByIdQuery(id), cancellationToken);
        return brand is null ? NotFoundProblem("Brand", id) : Ok(brand);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(BrandDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<BrandDetailsDto>> CreateAsync(
        [FromBody] CreateBrandRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _brandService.CreateAsync(
            new CreateBrandCommand(request.Code, request.WebsiteUrl, request.LogoMediaAssetId, request.SortOrder),
            cancellationToken);

        return CreatedAtRoute("GetAdminBrandById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(BrandDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDetailsDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateBrandRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _brandService.UpdateAsync(
            new UpdateBrandCommand(id, request.Code, request.WebsiteUrl, request.LogoMediaAssetId, request.SortOrder, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Brand", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(BrandDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _brandService.ArchiveAsync(new ArchiveBrandCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("Brand", id) : Ok(archived);
    }

    [HttpPut("{id:guid}/translations/{cultureCode}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(BrandTranslationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandTranslationDto>> UpsertTranslationAsync(
        Guid id,
        [FromRoute][StringLength(16, MinimumLength = 2)] string cultureCode,
        [FromBody] UpsertBrandTranslationRequest request,
        CancellationToken cancellationToken)
    {
        var translation = await _brandService.UpsertTranslationAsync(
            new UpsertBrandTranslationCommand(id, cultureCode, request.Name, request.Slug, request.Description),
            cancellationToken);

        return translation is null ? NotFoundProblem("Brand", id) : Ok(translation);
    }
}
