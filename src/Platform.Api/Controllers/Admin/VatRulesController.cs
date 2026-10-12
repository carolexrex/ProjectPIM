using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Catalog.Tax;
using Platform.Application.Catalog.Tax.Commands;
using Platform.Application.Catalog.Tax.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Tax;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.PricingRead)]
[Route("api/admin/vat-rules")]
public sealed class VatRulesController : ApiControllerBase
{
    private readonly IVatRuleAdminApplicationService _vatRuleService;

    public VatRulesController(IVatRuleAdminApplicationService vatRuleService)
    {
        _vatRuleService = vatRuleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VatRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<VatRuleDto>>> ListAsync(
        [FromQuery] Guid? marketId,
        [FromQuery] string? taxCategoryCode,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _vatRuleService.ListAsync(
            new ListVatRulesQuery(marketId, taxCategoryCode, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminVatRuleById")]
    [ProducesResponseType(typeof(VatRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VatRuleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var vatRule = await _vatRuleService.GetByIdAsync(new GetVatRuleByIdQuery(id), cancellationToken);
        return vatRule is null ? NotFoundProblem("VatRule", id) : Ok(vatRule);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(VatRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VatRuleDto>> CreateAsync([FromBody] CreateVatRuleRequest request, CancellationToken cancellationToken)
    {
        var created = await _vatRuleService.CreateAsync(
            new CreateVatRuleCommand(request.MarketId, request.TaxCategoryCode, request.Rate!.Value, request.Description),
            cancellationToken);

        return CreatedAtRoute("GetAdminVatRuleById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(VatRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VatRuleDto>> UpdateAsync(Guid id, [FromBody] UpdateVatRuleRequest request, CancellationToken cancellationToken)
    {
        var updated = await _vatRuleService.UpdateAsync(
            new UpdateVatRuleCommand(id, request.Rate!.Value, request.Description, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("VatRule", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteAsync(Guid id, [FromBody] DeleteVatRuleRequest request, CancellationToken cancellationToken)
    {
        var deleted = await _vatRuleService.DeleteAsync(new DeleteVatRuleCommand(id, request.RowVersion), cancellationToken);
        return deleted ? NoContent() : NotFoundProblem("VatRule", id);
    }
}
