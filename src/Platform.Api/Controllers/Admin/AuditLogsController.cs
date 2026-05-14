using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Auditing;
using Platform.Application.Auditing.Queries;
using Platform.Application.Security;
using Platform.Contracts.Common;
using Platform.Contracts.Security;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AuditRead)]
[Route("api/admin/audit-logs")]
public sealed class AuditLogsController : ApiControllerBase
{
    private readonly IAuditLogApplicationService _auditLogService;

    public AuditLogsController(IAuditLogApplicationService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AuditLogSummaryDto>>> ListAsync(
        [FromQuery] string? entityType,
        [FromQuery] string? actorIdentifier,
        [FromQuery] string? action,
        [FromQuery] DateTime? occurredFromUtc,
        [FromQuery] DateTime? occurredToUtc,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditLogService.ListAsync(
            new ListAuditLogsQuery(entityType, actorIdentifier, action, occurredFromUtc, occurredToUtc, page, pageSize, sort),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAuditLogById")]
    [ProducesResponseType(typeof(AuditLogDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var auditLog = await _auditLogService.GetByIdAsync(new GetAuditLogByIdQuery(id), cancellationToken);
        return auditLog is null ? NotFoundProblem("AuditLog", id) : Ok(auditLog);
    }
}
