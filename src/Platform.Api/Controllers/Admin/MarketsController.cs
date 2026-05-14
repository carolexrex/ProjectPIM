using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Markets.Commands;
using Platform.Application.Catalog.Markets.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Markets;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/markets")]
public sealed class MarketsController : ApiControllerBase
{
    private readonly IMarketAdminApplicationService _marketService;

    public MarketsController(IMarketAdminApplicationService marketService)
    {
        _marketService = marketService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MarketSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MarketSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _marketService.ListAsync(new ListMarketsQuery(search, status, page, pageSize, sort), cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<MarketLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MarketLookupDto>>> LookupAsync(
        [FromQuery] string? search,
        [FromQuery] string? status = "Active",
        [FromQuery] string? currencyCode = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _marketService.ListLookupsAsync(
            new ListMarketLookupsQuery(search, status, currencyCode),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminMarketById")]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var market = await _marketService.GetByIdAsync(new GetMarketByIdQuery(id), cancellationToken);
        return market is null ? NotFoundProblem("Market", id) : Ok(market);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MarketDetailsDto>> CreateAsync([FromBody] CreateMarketRequest request, CancellationToken cancellationToken)
    {
        var created = await _marketService.CreateAsync(
            new CreateMarketCommand(request.Code, request.Name, request.DefaultCurrency, request.DefaultCulture, request.VatMode),
            cancellationToken);

        return CreatedAtRoute("GetAdminMarketById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketDetailsDto>> UpdateAsync(Guid id, [FromBody] UpdateMarketRequest request, CancellationToken cancellationToken)
    {
        var updated = await _marketService.UpdateAsync(
            new UpdateMarketCommand(id, request.Code, request.Name, request.DefaultCurrency, request.DefaultCulture, request.VatMode, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Market", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketDetailsDto>> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _marketService.ArchiveAsync(new ArchiveMarketCommand(id), cancellationToken);
        return archived is null ? NotFoundProblem("Market", id) : Ok(archived);
    }

    [HttpPut("{id:guid}/currencies")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketDetailsDto>> AssignCurrenciesAsync(Guid id, [FromBody] AssignMarketCurrenciesRequest request, CancellationToken cancellationToken)
    {
        var updated = await _marketService.AssignCurrenciesAsync(
            new AssignMarketCurrenciesCommand(id, request.DefaultCurrency, request.Currencies, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Market", id) : Ok(updated);
    }

    [HttpPut("{id:guid}/cultures")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketDetailsDto>> AssignCulturesAsync(Guid id, [FromBody] AssignMarketCulturesRequest request, CancellationToken cancellationToken)
    {
        var updated = await _marketService.AssignCulturesAsync(
            new AssignMarketCulturesCommand(id, request.DefaultCulture, request.Cultures, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Market", id) : Ok(updated);
    }

    [HttpPut("{marketId:guid}/products/{productId:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketDetailsDto>> UpsertProductAssignmentAsync(
        Guid marketId,
        Guid productId,
        [FromBody] UpsertMarketProductAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _marketService.UpsertProductAssignmentAsync(
            new UpsertMarketProductAssignmentCommand(marketId, productId, request.Status, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Market", marketId) : Ok(updated);
    }

    [HttpPost("{marketId:guid}/products/{productId:guid}/remove")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MarketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketDetailsDto>> RemoveProductAssignmentAsync(
        Guid marketId,
        Guid productId,
        [FromBody] RemoveMarketProductAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _marketService.RemoveProductAssignmentAsync(
            new RemoveMarketProductAssignmentCommand(marketId, productId, request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("Market", marketId) : Ok(updated);
    }
}
