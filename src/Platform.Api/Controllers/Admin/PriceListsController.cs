using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Pricing.Commands;
using Platform.Application.Catalog.Pricing.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Pricing;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.PricingRead)]
[Route("api/admin/price-lists")]
public sealed class PriceListsController : ApiControllerBase
{
    private readonly IPriceListAdminApplicationService _priceListService;

    public PriceListsController(IPriceListAdminApplicationService priceListService)
    {
        _priceListService = priceListService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PriceListSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PriceListSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? currencyCode,
        [FromQuery] string? status,
        [FromQuery] Guid? marketId,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _priceListService.ListAsync(
            new ListPriceListsQuery(search, currencyCode, status, marketId, page, pageSize, sort),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminPriceListById")]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var priceList = await _priceListService.GetByIdAsync(new GetPriceListByIdQuery(id), cancellationToken);
        return priceList is null ? NotFoundProblem("PriceList", id) : Ok(priceList);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PriceListDetailsDto>> CreateAsync([FromBody] CreatePriceListRequest request, CancellationToken cancellationToken)
    {
        var created = await _priceListService.CreateAsync(
            new CreatePriceListCommand(request.Code, request.Name, request.CurrencyCode, request.VatIncluded, request.ValidFromUtc, request.ValidToUtc),
            cancellationToken);

        return CreatedAtRoute("GetAdminPriceListById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailsDto>> UpdateAsync(Guid id, [FromBody] UpdatePriceListRequest request, CancellationToken cancellationToken)
    {
        var updated = await _priceListService.UpdateAsync(
            new UpdatePriceListCommand(id, request.Code, request.Name, request.CurrencyCode, request.VatIncluded, request.ValidFromUtc, request.ValidToUtc, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("PriceList", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _priceListService.ArchiveAsync(new ArchivePriceListCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("PriceList", id) : Ok(archived);
    }

    [HttpPost("{id:guid}/markets")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailsDto>> UpsertMarketAssignmentAsync(Guid id, [FromBody] UpsertPriceListMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        var updated = await _priceListService.UpsertMarketAssignmentAsync(
            new UpsertPriceListMarketAssignmentCommand(id, request.MarketId, request.Priority, request.IsBasePriceList, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("PriceList", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/markets/{marketId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailsDto>> RemoveMarketAssignmentAsync(Guid id, Guid marketId, [FromBody] RemovePriceListMarketAssignmentRequest request, CancellationToken cancellationToken)
    {
        var updated = await _priceListService.RemoveMarketAssignmentAsync(
            new RemovePriceListMarketAssignmentCommand(id, marketId, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("PriceList", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/entries")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailsDto>> UpsertEntryAsync(Guid id, [FromBody] UpsertPriceListEntryRequest request, CancellationToken cancellationToken)
    {
        var updated = await _priceListService.UpsertEntryAsync(
            new UpsertPriceListEntryCommand(
                id,
                request.EntryId,
                request.TargetType,
                request.TargetId,
                request.MinQuantity,
                request.Amount,
                request.CompareAtAmount,
                request.ValidFromUtc,
                request.ValidToUtc,
                request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("PriceList", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/entries/{entryId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.PricingWrite)]
    [ProducesResponseType(typeof(PriceListDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailsDto>> RemoveEntryAsync(Guid id, Guid entryId, [FromBody] RemovePriceListEntryRequest request, CancellationToken cancellationToken)
    {
        var updated = await _priceListService.RemoveEntryAsync(
            new RemovePriceListEntryCommand(id, entryId, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("PriceList", id) : Ok(updated);
    }
}
