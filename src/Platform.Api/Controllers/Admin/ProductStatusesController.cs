using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Catalog.Products;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Products;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/product-statuses")]
public sealed class ProductStatusesController : ControllerBase
{
    private readonly IProductStatusDefinitionApplicationService _service;

    public ProductStatusesController(IProductStatusDefinitionApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductStatusDto>>> ListAsync(
        [FromQuery] string entityType = "product",
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ListAsync(entityType, cancellationToken));
    }
}
