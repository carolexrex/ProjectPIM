using Microsoft.AspNetCore.Mvc;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;

namespace Platform.StorefrontApi.Controllers;

[ApiController]
[Route("api/storefront/carts")]
public sealed class StorefrontCartsController : ControllerBase
{
    private const string CartAccessTokenHeaderName = "X-Storefront-Cart-Token";

    private readonly IStorefrontCartApplicationService _cartService;

    public StorefrontCartsController(IStorefrontCartApplicationService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StorefrontCartDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontCartDto>> CreateAsync(
        [FromBody] CreateStorefrontCartRequest request,
        [FromQuery] string? channel = null,
        [FromQuery] string? market = null,
        [FromQuery] string? culture = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cartService.CreateAsync(
            new CreateStorefrontCartCommand(
                channel,
                market,
                culture,
                currency,
                Request.Host.Host,
                request.Email,
                request.Lines.Select(x => new CreateStorefrontCartLineItem(x.VariantId, x.Quantity, x.Comment)).ToList(),
                request.Addresses.Select(x => new CreateStorefrontCartAddressItem(
                    x.Type,
                    x.FirstName,
                    x.LastName,
                    x.CompanyName,
                    x.Line1,
                    x.Line2,
                    x.PostalCode,
                    x.City,
                    x.Region,
                    x.CountryCode,
                    x.Email,
                    x.Phone)).ToList()),
            cancellationToken);

        return ToCartActionResult(result, created: true);
    }

    [HttpGet("{id:guid}", Name = "GetStorefrontCartById")]
    [ProducesResponseType(typeof(StorefrontCartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontCartDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cartService.GetByIdAsync(new GetStorefrontCartByIdQuery(id, GetCartAccessToken()), cancellationToken);
        return ToCartActionResult(result, created: false);
    }

    [HttpPost("{id:guid}/reprice")]
    [ProducesResponseType(typeof(StorefrontCartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontCartDto>> RepriceAsync(
        Guid id,
        [FromBody] RepriceStorefrontCartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.RepriceAsync(
            new RepriceStorefrontCartCommand(id, request.RowVersion, GetCartAccessToken()),
            cancellationToken);

        return ToCartActionResult(result, created: false);
    }

    [HttpPost("{id:guid}/checkout")]
    [ProducesResponseType(typeof(StorefrontOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontOrderDto>> CheckoutAsync(
        Guid id,
        [FromBody] CheckoutStorefrontCartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.CheckoutAsync(
            new CheckoutStorefrontCartCommand(id, request.RowVersion, GetCartAccessToken()),
            cancellationToken);

        SetNoStoreHeaders();

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success => Ok(result.Order),
            StorefrontContextResolutionStatus.Unauthorized => UnauthorizedProblem(),
            StorefrontContextResolutionStatus.NotFound => NotFoundProblem(result.ResourceName, result.ResourceKey),
            StorefrontContextResolutionStatus.ValidationFailed => BadRequest(new HttpValidationProblemDetails(result.Errors)),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected storefront checkout result.")
        };
    }

    private ActionResult<StorefrontCartDto> ToCartActionResult(StorefrontCartResult result, bool created)
    {
        SetNoStoreHeaders();

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success when created => CreatedAtRoute(
                "GetStorefrontCartById",
                new { id = result.Cart!.Id },
                result.Cart),
            StorefrontContextResolutionStatus.Success => Ok(result.Cart),
            StorefrontContextResolutionStatus.Unauthorized => UnauthorizedProblem(),
            StorefrontContextResolutionStatus.NotFound => NotFoundProblem(result.ResourceName, result.ResourceKey),
            StorefrontContextResolutionStatus.ValidationFailed => BadRequest(new HttpValidationProblemDetails(result.Errors)),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected storefront cart result.")
        };
    }

    private string? GetCartAccessToken()
    {
        return Request.Headers.TryGetValue(CartAccessTokenHeaderName, out var token)
            ? token.FirstOrDefault()
            : null;
    }

    private ActionResult UnauthorizedProblem()
    {
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Cart ownership proof is required.",
            Type = "https://httpstatuses.com/401",
            Detail = $"Pass the cart access token in the {CartAccessTokenHeaderName} header."
        });
    }

    private void SetNoStoreHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
    }

    private ActionResult NotFoundProblem(string? resourceName, string? resourceKey)
    {
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found.",
            Type = "https://httpstatuses.com/404",
            Detail = $"{resourceName ?? "Resource"} '{resourceKey ?? string.Empty}' was not found."
        });
    }
}
