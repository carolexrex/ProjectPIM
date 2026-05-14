using Platform.Application.Catalog.Media.Queries;
using Platform.Domain.Catalog.Media;

namespace Platform.Application.Catalog.Media;

public interface IMediaAssetRepository
{
    Task<MediaAssetListResult> ListAsync(ListMediaAssetsQuery query, CancellationToken cancellationToken);
    Task<MediaAsset?> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaAsset>> GetByIdsAsync(IReadOnlyCollection<Guid> mediaAssetIds, CancellationToken cancellationToken);
    Task<MediaAsset?> GetByStorageKeyAsync(string storageKey, CancellationToken cancellationToken);
    Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);
}
