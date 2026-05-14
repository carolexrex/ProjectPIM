using Microsoft.AspNetCore.Mvc;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;

namespace Platform.StorefrontApi.Controllers;

[ApiController]
[Route("api/storefront/products")]
public sealed class StorefrontProductsController : ControllerBase
{
    private readonly IStorefrontProductApplicationService _productService;

    public StorefrontProductsController(IStorefrontProductApplicationService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StorefrontProductListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontProductListResponseDto>> ListAsync(
        [FromQuery] string? channel = null,
        [FromQuery] string? market = null,
        [FromQuery] string? culture = null,
        [FromQuery] string? currency = null,
        [FromQuery(Name = "category")] string? categorySlug = null,
        [FromQuery(Name = "brand")] string? brandCode = null,
        [FromQuery(Name = "q")] string? query = null,
        [FromQuery] string? sort = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.ListAsync(
            new GetStorefrontProductsQuery(
                channel,
                market,
                culture,
                currency,
                Request.Host.Host,
                categorySlug,
                brandCode,
                query,
                sort,
                page,
                pageSize),
            cancellationToken);

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success => Ok(result.Products),
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
                title: "Unexpected storefront product list result.")
        };
    }

    [HttpGet("by-number/{productNumber}")]
    [ProducesResponseType(typeof(StorefrontProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontProductDetailsDto>> GetByProductNumberAsync(
        string productNumber,
        [FromQuery] string? channel = null,
        [FromQuery] string? market = null,
        [FromQuery] string? culture = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetByProductNumberAsync(
            new GetStorefrontProductByProductNumberQuery(
                productNumber,
                channel,
                market,
                culture,
                currency,
                Request.Host.Host),
            cancellationToken);

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success => Ok(result.Product),
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
                title: "Unexpected storefront product details result.")
        };
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(StorefrontProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontProductDetailsDto>> GetBySlugAsync(
        string slug,
        [FromQuery] string? channel = null,
        [FromQuery] string? market = null,
        [FromQuery] string? culture = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetBySlugAsync(
            new GetStorefrontProductBySlugQuery(
                slug,
                channel,
                market,
                culture,
                currency,
                Request.Host.Host),
            cancellationToken);

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success => Ok(result.Product),
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
                title: "Unexpected storefront product details result.")
        };
    }
}
