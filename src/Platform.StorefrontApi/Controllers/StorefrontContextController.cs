using Microsoft.AspNetCore.Mvc;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;

namespace Platform.StorefrontApi.Controllers;

[ApiController]
[Route("api/storefront/context")]
public sealed class StorefrontContextController : ControllerBase
{
    private readonly IStorefrontContextApplicationService _contextService;

    public StorefrontContextController(IStorefrontContextApplicationService contextService)
    {
        _contextService = contextService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StorefrontContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorefrontContextDto>> GetAsync(
        [FromQuery] string? channel = null,
        [FromQuery] string? market = null,
        [FromQuery] string? culture = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _contextService.GetContextAsync(
            new GetStorefrontContextQuery(
                channel,
                market,
                culture,
                currency,
                Request.Host.Host),
            cancellationToken);

        return result.Status switch
        {
            StorefrontContextResolutionStatus.Success => Ok(result.Context),
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
                title: "Unexpected storefront context resolution result.")
        };
    }
}
