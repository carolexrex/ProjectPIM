using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Media.Commands;
using Platform.Application.Catalog.Media.Queries;
using Platform.Contracts.Catalog.Media;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Media;

namespace Platform.Infrastructure.Catalog.Media;

public sealed class MediaAssetAdminApplicationService : IMediaAssetAdminApplicationService
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MediaAssetAdminApplicationService(IMediaAssetRepository mediaAssetRepository, IUnitOfWork unitOfWork)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<MediaAssetSummaryDto>> ListAsync(ListMediaAssetsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediaAssetRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<MediaAssetSummaryDto>(result.Items.Select(MapSummary).ToList(), result.Total, page, pageSize);
    }

    public async Task<MediaAssetDetailsDto?> GetByIdAsync(GetMediaAssetByIdQuery query, CancellationToken cancellationToken)
    {
        var mediaAsset = await _mediaAssetRepository.GetByIdAsync(query.MediaAssetId, cancellationToken);
        return mediaAsset is null ? null : MapDetails(mediaAsset);
    }

    public async Task<MediaAssetDetailsDto> CreateAsync(CreateMediaAssetCommand command, CancellationToken cancellationToken)
    {
        if (await _mediaAssetRepository.GetByStorageKeyAsync(command.StorageKey, cancellationToken) is not null)
        {
            throw new ConflictException("Media asset storage key already exists.");
        }

        var now = DateTime.UtcNow;
        var mediaAsset = new MediaAsset(
            Guid.NewGuid(),
            command.StorageProvider,
            command.StorageKey,
            command.FileName,
            command.ContentType,
            command.FileSize,
            command.Width,
            command.Height,
            command.PublicUrl,
            command.AltText,
            command.Title,
            now,
            now);

        await _mediaAssetRepository.AddAsync(mediaAsset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(mediaAsset);
    }

    public async Task<MediaAssetDetailsDto?> UpdateAsync(UpdateMediaAssetCommand command, CancellationToken cancellationToken)
    {
        var mediaAsset = await _mediaAssetRepository.GetByIdAsync(command.MediaAssetId, cancellationToken);
        if (mediaAsset is null)
        {
            return null;
        }

        mediaAsset.Update(
            command.FileName,
            command.ContentType,
            command.FileSize,
            command.Width,
            command.Height,
            command.PublicUrl,
            command.AltText,
            command.Title,
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(mediaAsset);
    }

    public async Task<MediaAssetDetailsDto?> ArchiveAsync(ArchiveMediaAssetCommand command, CancellationToken cancellationToken)
    {
        var mediaAsset = await _mediaAssetRepository.GetByIdAsync(command.MediaAssetId, cancellationToken);
        if (mediaAsset is null)
        {
            return null;
        }

        mediaAsset.Archive(command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(mediaAsset);
    }

    private static MediaAssetSummaryDto MapSummary(MediaAsset mediaAsset)
    {
        return new MediaAssetSummaryDto(
            mediaAsset.Id,
            mediaAsset.FileName,
            mediaAsset.ContentType,
            mediaAsset.PublicUrl,
            mediaAsset.Title,
            mediaAsset.AltText,
            mediaAsset.Status,
            mediaAsset.UpdatedAtUtc,
            mediaAsset.RowVersion);
    }

    private static MediaAssetDetailsDto MapDetails(MediaAsset mediaAsset)
    {
        return new MediaAssetDetailsDto(
            mediaAsset.Id,
            mediaAsset.StorageProvider,
            mediaAsset.StorageKey,
            mediaAsset.FileName,
            mediaAsset.ContentType,
            mediaAsset.FileSize,
            mediaAsset.Width,
            mediaAsset.Height,
            mediaAsset.PublicUrl,
            mediaAsset.Title,
            mediaAsset.AltText,
            mediaAsset.Status,
            mediaAsset.CreatedAtUtc,
            mediaAsset.UpdatedAtUtc,
            mediaAsset.RowVersion);
    }
}
