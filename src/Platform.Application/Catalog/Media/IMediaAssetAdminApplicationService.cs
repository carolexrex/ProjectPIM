using Platform.Application.Catalog.Media.Commands;
using Platform.Application.Catalog.Media.Queries;
using Platform.Contracts.Catalog.Media;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Media;

public interface IMediaAssetAdminApplicationService
{
    Task<PagedResponse<MediaAssetSummaryDto>> ListAsync(ListMediaAssetsQuery query, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto?> GetByIdAsync(GetMediaAssetByIdQuery query, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto> CreateAsync(CreateMediaAssetCommand command, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto?> UpdateAsync(UpdateMediaAssetCommand command, CancellationToken cancellationToken);
    Task<MediaAssetDetailsDto?> ArchiveAsync(ArchiveMediaAssetCommand command, CancellationToken cancellationToken);
}
