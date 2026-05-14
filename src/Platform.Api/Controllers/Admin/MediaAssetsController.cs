using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Controllers;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Media.Commands;
using Platform.Application.Catalog.Media.Queries;
using Platform.Application.Security;
using Platform.Contracts.Catalog.Media;
using Platform.Contracts.Common;

namespace Platform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.CatalogRead)]
[Route("api/admin/media-assets")]
public sealed class MediaAssetsController : ApiControllerBase
{
    private readonly IMediaAssetAdminApplicationService _mediaAssetService;

    public MediaAssetsController(IMediaAssetAdminApplicationService mediaAssetService)
    {
        _mediaAssetService = mediaAssetService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MediaAssetSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MediaAssetSummaryDto>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? contentType,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, 500)] int pageSize = 50,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediaAssetService.ListAsync(
            new ListMediaAssetsQuery(search, status, contentType, page, pageSize, sort),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetAdminMediaAssetById")]
    [ProducesResponseType(typeof(MediaAssetDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediaAssetService.GetByIdAsync(new GetMediaAssetByIdQuery(id), cancellationToken);
        return result is null ? NotFoundProblem("MediaAsset", id) : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MediaAssetDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MediaAssetDetailsDto>> CreateAsync(
        [FromBody] CreateMediaAssetRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _mediaAssetService.CreateAsync(
            new CreateMediaAssetCommand(
                request.StorageProvider,
                request.StorageKey,
                request.FileName,
                request.ContentType,
                request.FileSize,
                request.Width,
                request.Height,
                request.PublicUrl,
                request.AltText,
                request.Title),
            cancellationToken);

        return CreatedAtRoute("GetAdminMediaAssetById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MediaAssetDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetDetailsDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateMediaAssetRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _mediaAssetService.UpdateAsync(
            new UpdateMediaAssetCommand(
                id,
                request.FileName,
                request.ContentType,
                request.FileSize,
                request.Width,
                request.Height,
                request.PublicUrl,
                request.AltText,
                request.Title,
                request.RowVersion),
            cancellationToken);

        return updated is null ? NotFoundProblem("MediaAsset", id) : Ok(updated);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AdminPolicies.CatalogWrite)]
    [ProducesResponseType(typeof(MediaAssetDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetDetailsDto>> ArchiveAsync(
        Guid id,
        [FromBody] ArchiveMediaAssetRequest request,
        CancellationToken cancellationToken)
    {
        var archived = await _mediaAssetService.ArchiveAsync(
            new ArchiveMediaAssetCommand(id, request.RowVersion),
            cancellationToken);

        return archived is null ? NotFoundProblem("MediaAsset", id) : Ok(archived);
    }
}
