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
[Route("api/admin/webhook-deliveries")]
public sealed class WebhookDeliveriesController : ApiControllerBase
{
    private readonly IWebhookAdminApplicationService _webhookAdminService;

    public WebhookDeliveriesController(IWebhookAdminApplicationService webhookAdminService)
    {
        _webhookAdminService = webhookAdminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<WebhookDeliverySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<WebhookDeliverySummaryDto>>> ListAsync(
        [FromQuery] Guid? webhookSubscriptionId,
        [FromQuery] string? eventType,
        [FromQuery] string? status,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _webhookAdminService.ListDeliveriesAsync(
            new ListWebhookDeliveriesQuery(webhookSubscriptionId, eventType, status, page, pageSize, sort),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetWebhookDeliveryById")]
    [ProducesResponseType(typeof(WebhookDeliveryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebhookDeliveryDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var delivery = await _webhookAdminService.GetDeliveryByIdAsync(new GetWebhookDeliveryByIdQuery(id), cancellationToken);
        return delivery is null ? NotFoundProblem("WebhookDelivery", id) : Ok(delivery);
    }

    [HttpPost("{id:guid}/replay")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(WebhookDeliveryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WebhookDeliveryDetailsDto>> ReplayAsync(
        Guid id,
        [FromBody] ReplayWebhookDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var replayed = await _webhookAdminService.ReplayDeliveryAsync(
            new ReplayWebhookDeliveryCommand(id, request.RowVersion),
            cancellationToken);
        return replayed is null ? NotFoundProblem("WebhookDelivery", id) : Ok(replayed);
    }
}
