using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Categories.Commands;
using Platform.Application.Catalog.Categories.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Categories;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/categories")]
public sealed class CategoriesController : ApiControllerBase
{
    private readonly ICategoryAdminApplicationService _categoryService;

    public CategoriesController(ICategoryAdminApplicationService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CategorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CategorySummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? parentCategoryId,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.ListAsync(
            new ListCategoriesQuery(search, status, parentCategoryId, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminCategoryById")]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(new GetCategoryByIdQuery(id), cancellationToken);
        return category is null ? NotFoundProblem("Category", id) : Ok(category);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryDetailsDto>> CreateAsync(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _categoryService.CreateAsync(
            new CreateCategoryCommand(request.Code, request.ParentCategoryId, request.SortOrder),
            cancellationToken);

        return CreatedAtRoute("GetAdminCategoryById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailsDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _categoryService.UpdateAsync(
            new UpdateCategoryCommand(id, request.Code, request.ParentCategoryId, request.SortOrder, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Category", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _categoryService.ArchiveAsync(new ArchiveCategoryCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("Category", id) : Ok(archived);
    }

    [HttpPut("{id:guid}/translations/{cultureCode}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(CategoryTranslationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryTranslationDto>> UpsertTranslationAsync(
        Guid id,
        [FromRoute][StringLength(16, MinimumLength = 2)] string cultureCode,
        [FromBody] UpsertCategoryTranslationRequest request,
        CancellationToken cancellationToken)
    {
        var translation = await _categoryService.UpsertTranslationAsync(
            new UpsertCategoryTranslationCommand(id, cultureCode, request.Name, request.Slug, request.Description),
            cancellationToken);

        return translation is null ? NotFoundProblem("Category", id) : Ok(translation);
    }
}
