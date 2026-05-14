using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Catalog.Inventory.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Inventory;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.InventoryRead)]
[Route("api/admin/inventory-locations")]
public sealed class InventoryLocationsController : ApiControllerBase
{
    private readonly IInventoryAdminApplicationService _inventoryService;

    public InventoryLocationsController(IInventoryAdminApplicationService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<InventoryLocationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<InventoryLocationSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? marketId,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.ListLocationsAsync(
            new ListInventoryLocationsQuery(search, status, marketId, page, pageSize, sort),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminInventoryLocationById")]
    [ProducesResponseType(typeof(InventoryLocationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryLocationDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await _inventoryService.GetLocationByIdAsync(new GetInventoryLocationByIdQuery(id), cancellationToken);
        return location is null ? NotFoundProblem("InventoryLocation", id) : Ok(location);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ProducesResponseType(typeof(InventoryLocationDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InventoryLocationDetailsDto>> CreateAsync([FromBody] CreateInventoryLocationRequest request, CancellationToken cancellationToken)
    {
        var created = await _inventoryService.CreateLocationAsync(
            new CreateInventoryLocationCommand(request.Code, request.Name, request.Type, request.CountryCode),
            cancellationToken);

        return CreatedAtRoute("GetAdminInventoryLocationById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ProducesResponseType(typeof(InventoryLocationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryLocationDetailsDto>> UpdateAsync(Guid id, [FromBody] UpdateInventoryLocationRequest request, CancellationToken cancellationToken)
    {
        var updated = await _inventoryService.UpdateLocationAsync(
            new UpdateInventoryLocationCommand(id, request.Code, request.Name, request.Type, request.CountryCode, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("InventoryLocation", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ProducesResponseType(typeof(InventoryLocationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryLocationDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _inventoryService.ArchiveLocationAsync(new ArchiveInventoryLocationCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("InventoryLocation", id) : Ok(archived);
    }

    [HttpPost("{id:guid}/markets")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ProducesResponseType(typeof(InventoryLocationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryLocationDetailsDto>> UpsertMarketAssignmentAsync(
        Guid id,
        [FromBody] UpsertInventoryLocationMarketAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _inventoryService.UpsertLocationMarketAssignmentAsync(
            new UpsertInventoryLocationMarketAssignmentCommand(id, request.MarketId, request.Priority, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("InventoryLocation", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/markets/{marketId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.InventoryWrite)]
    [ProducesResponseType(typeof(InventoryLocationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryLocationDetailsDto>> RemoveMarketAssignmentAsync(
        Guid id,
        Guid marketId,
        [FromBody] RemoveInventoryLocationMarketAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _inventoryService.RemoveLocationMarketAssignmentAsync(
            new RemoveInventoryLocationMarketAssignmentCommand(id, marketId, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("InventoryLocation", id) : Ok(updated);
    }
}
