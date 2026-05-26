using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Application.Security;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/storefront-projection-refresh-messages")]
public sealed class StorefrontProjectionRefreshMessagesController : ApiControllerBase
{
    private readonly IStorefrontProjectionRefreshMessageAdminApplicationService _storefrontRefreshMessageService;

    public StorefrontProjectionRefreshMessagesController(
        IStorefrontProjectionRefreshMessageAdminApplicationService storefrontRefreshMessageService)
    {
        _storefrontRefreshMessageService = storefrontRefreshMessageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>>> ListAsync(
        [FromQuery] string? status,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _storefrontRefreshMessageService.ListAsync(
            new ListStorefrontProjectionRefreshMessagesQuery(status, page, pageSize, sort),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetStorefrontProjectionRefreshMessageById")]
    [ProducesResponseType(typeof(StorefrontProjectionRefreshMessageDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorefrontProjectionRefreshMessageDetailsDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var message = await _storefrontRefreshMessageService.GetByIdAsync(
            new GetStorefrontProjectionRefreshMessageByIdQuery(id),
            cancellationToken);
        return message is null ? NotFoundProblem("StorefrontProjectionRefreshMessage", id) : Ok(message);
    }

    [HttpPost("{id:guid}/reset")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(StorefrontProjectionRefreshMessageDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorefrontProjectionRefreshMessageDetailsDto>> ResetAsync(
        Guid id,
        [FromBody] ResetStorefrontProjectionRefreshMessageRequest request,
        CancellationToken cancellationToken)
    {
        var reset = await _storefrontRefreshMessageService.ResetAsync(
            new ResetStorefrontProjectionRefreshMessageCommand(id, request.RowVersion),
            cancellationToken);
        return reset is null ? NotFoundProblem("StorefrontProjectionRefreshMessage", id) : Ok(reset);
    }
}
