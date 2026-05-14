using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Queries;
using Platform.Application.Security;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/integration-jobs")]
public sealed class IntegrationJobsController : ApiControllerBase
{
    private readonly IIntegrationJobAdminApplicationService _integrationJobService;

    public IntegrationJobsController(IIntegrationJobAdminApplicationService integrationJobService)
    {
        _integrationJobService = integrationJobService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<IntegrationJobSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<IntegrationJobSummaryDto>>> ListAsync(
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] string? requestedBy,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _integrationJobService.ListAsync(
            new ListIntegrationJobsQuery(type, status, requestedBy, page, pageSize, sort),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetIntegrationJobById")]
    [ProducesResponseType(typeof(IntegrationJobDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationJobDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await _integrationJobService.GetByIdAsync(new GetIntegrationJobByIdQuery(id), cancellationToken);
        return job is null ? NotFoundProblem("IntegrationJob", id) : Ok(job);
    }
}
