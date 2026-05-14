using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Inventory;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.InventoryWrite)]
[Route("api/admin/inventory-balances")]
public sealed class InventoryBalancesController : ControllerBase
{
    private readonly IInventoryAdminApplicationService _inventoryService;

    public InventoryBalancesController(IInventoryAdminApplicationService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPut]
    [ProducesResponseType(typeof(InventoryBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryBalanceDto>> UpsertAsync([FromBody] UpsertInventoryBalanceRequest request, CancellationToken cancellationToken)
    {
        var updated = await _inventoryService.UpsertBalanceAsync(
            new UpsertInventoryBalanceCommand(
                request.InventoryLocationId,
                request.VariantId,
                request.OnHandQuantity,
                request.ReservedQuantity,
                request.IncomingQuantity,
                request.Backorderable,
                request.RowVersion),
            cancellationToken);

        return Ok(updated);
    }
}
