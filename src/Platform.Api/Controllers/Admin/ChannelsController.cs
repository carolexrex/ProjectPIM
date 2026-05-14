using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Channels;
using Platform.Application.Catalog.Channels.Commands;
using Platform.Application.Catalog.Channels.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Channels;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/channels")]
public sealed class ChannelsController : ApiControllerBase
{
    private readonly IChannelAdminApplicationService _channelService;

    public ChannelsController(IChannelAdminApplicationService channelService)
    {
        _channelService = channelService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ChannelSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ChannelSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _channelService.ListAsync(new ListChannelsQuery(search, status, page, pageSize, sort), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminChannelById")]
    [ProducesResponseType(typeof(ChannelDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetByIdAsync(new GetChannelByIdQuery(id), cancellationToken);
        return channel is null ? NotFoundProblem("Channel", id) : Ok(channel);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ChannelDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ChannelDetailsDto>> CreateAsync([FromBody] CreateChannelRequest request, CancellationToken cancellationToken)
    {
        var created = await _channelService.CreateAsync(new CreateChannelCommand(request.Code, request.Name, request.HostName), cancellationToken);
        return CreatedAtRoute("GetAdminChannelById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ChannelDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelDetailsDto>> UpdateAsync(Guid id, [FromBody] UpdateChannelRequest request, CancellationToken cancellationToken)
    {
        var updated = await _channelService.UpdateAsync(new UpdateChannelCommand(id, request.Code, request.Name, request.HostName, request.RowVersion), cancellationToken);
        return updated is null ? NotFoundProblem("Channel", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ChannelDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _channelService.ArchiveAsync(new ArchiveChannelCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("Channel", id) : Ok(archived);
    }

    [HttpPost("{channelId:guid}/markets")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ChannelDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelDetailsDto>> UpsertMarketAssignmentAsync(
        Guid channelId,
        [FromBody] UpsertChannelMarketAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _channelService.UpsertMarketAssignmentAsync(
            new UpsertChannelMarketAssignmentCommand(channelId, request.MarketId, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Channel", channelId) : Ok(updated);
    }

    [HttpPost("{channelId:guid}/markets/{marketId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(ChannelDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelDetailsDto>> RemoveMarketAssignmentAsync(
        Guid channelId,
        Guid marketId,
        [FromBody] RemoveChannelMarketAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _channelService.RemoveMarketAssignmentAsync(
            new RemoveChannelMarketAssignmentCommand(channelId, marketId, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Channel", channelId) : Ok(updated);
    }
}
