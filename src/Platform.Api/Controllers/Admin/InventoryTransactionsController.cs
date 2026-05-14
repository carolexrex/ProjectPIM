using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Inventory;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.InventoryWrite)]
[Route("api/admin/inventory-transactions")]
public sealed class InventoryTransactionsController : ControllerBase
{
    private readonly IInventoryAdminApplicationService _inventoryService;

    public InventoryTransactionsController(IInventoryAdminApplicationService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(InventoryTransactionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryTransactionDto>> AdjustAsync([FromBody] AdjustInventoryRequest request, CancellationToken cancellationToken)
    {
        var transaction = await _inventoryService.AdjustInventoryAsync(
            new AdjustInventoryCommand(
                request.InventoryLocationId,
                request.VariantId,
                request.Type,
                request.QuantityDelta,
                request.ReferenceType,
                request.ReferenceId),
            cancellationToken);

        return Ok(transaction);
    }
}
