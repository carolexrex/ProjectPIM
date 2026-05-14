using Microsoft.AspNetCore.Mvc;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;

namespace Platform.StorefrontApi.Controllers;

[ApiController]
[Route("api/storefront/categories")]
public sealed class StorefrontCategoriesController : ControllerBase
{
    private readonly IStorefrontCategoryApplicationService _categoryService;

    public StorefrontCategoriesController(IStorefrontCategoryApplicationService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StorefrontCategoryNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StorefrontCategoryNodeDto>>> ListAsync(
        [FromQuery] string? channel = null,
        [FromQuery] string? market = null,
        [FromQuery] string? culture = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.ListAsync(
            new GetStorefrontCategoriesQuery(channel, market, culture, currency, Request.Host.Host),
            cancellationToken);

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success => Ok(result.Categories),
            StorefrontContextResolutionStatus.NotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Type = "https://httpstatuses.com/404",
                Detail = $"{result.ResourceName} '{result.ResourceKey}' was not found."
            }),
            StorefrontContextResolutionStatus.ValidationFailed => BadRequest(new HttpValidationProblemDetails(result.Errors)),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected storefront category list result.")
        };
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(StorefrontCategoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontCategoryDetailsDto>> GetBySlugAsync(
        string slug,
        [FromQuery] string? channel = null,
        [FromQuery] string? market = null,
        [FromQuery] string? culture = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetBySlugAsync(
            new GetStorefrontCategoryBySlugQuery(slug, channel, market, culture, currency, Request.Host.Host),
            cancellationToken);

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success => Ok(result.Category),
            StorefrontContextResolutionStatus.NotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Type = "https://httpstatuses.com/404",
                Detail = $"{result.ResourceName} '{result.ResourceKey}' was not found."
            }),
            StorefrontContextResolutionStatus.ValidationFailed => BadRequest(new HttpValidationProblemDetails(result.Errors)),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected storefront category details result.")
        };
    }
}
