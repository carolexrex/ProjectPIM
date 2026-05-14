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
[Route("api/admin/webhook-subscriptions")]
public sealed class WebhookSubscriptionsController : ApiControllerBase
{
    private readonly IWebhookAdminApplicationService _webhookAdminService;

    public WebhookSubscriptionsController(IWebhookAdminApplicationService webhookAdminService)
    {
        _webhookAdminService = webhookAdminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<WebhookSubscriptionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<WebhookSubscriptionSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? eventType,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _webhookAdminService.ListSubscriptionsAsync(
            new ListWebhookSubscriptionsQuery(search, isActive, eventType, page, pageSize, sort),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetWebhookSubscriptionById")]
    [ProducesResponseType(typeof(WebhookSubscriptionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebhookSubscriptionDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var subscription = await _webhookAdminService.GetSubscriptionByIdAsync(new GetWebhookSubscriptionByIdQuery(id), cancellationToken);
        return subscription is null ? NotFoundProblem("WebhookSubscription", id) : Ok(subscription);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(WebhookSubscriptionDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WebhookSubscriptionDetailsDto>> CreateAsync(
        [FromBody] CreateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _webhookAdminService.CreateSubscriptionAsync(
            new CreateWebhookSubscriptionCommand(request.Name, request.EndpointUrl, request.Secret, request.EventTypes, request.IsActive),
            cancellationToken);
        return CreatedAtRoute("GetWebhookSubscriptionById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(WebhookSubscriptionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebhookSubscriptionDetailsDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _webhookAdminService.UpdateSubscriptionAsync(
            new UpdateWebhookSubscriptionCommand(id, request.Name, request.EndpointUrl, request.Secret, request.EventTypes, request.IsActive, request.RowVersion),
            cancellationToken);
        return updated is null ? NotFoundProblem("WebhookSubscription", id) : Ok(updated);
    }
}
