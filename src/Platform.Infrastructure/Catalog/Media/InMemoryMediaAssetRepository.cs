using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Media.Queries;
using Platform.Domain.Catalog.Media;

namespace Platform.Infrastructure.Catalog.Media;

public sealed class InMemoryMediaAssetRepository : IMediaAssetRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryMediaAssetRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<MediaAssetListResult> ListAsync(ListMediaAssetsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
                _store.MediaAssets.Values
                    .Where(x => string.IsNullOrWhiteSpace(query.Search)
                        || x.FileName.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                        || x.Title != null && x.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                        || x.PublicUrl.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                    .Where(x => string.IsNullOrWhiteSpace(query.Status) || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
                    .Where(x => string.IsNullOrWhiteSpace(query.ContentType) || string.Equals(x.ContentType, query.ContentType, StringComparison.OrdinalIgnoreCase)),
                query.Sort)
            .ToList();

        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new MediaAssetListResult(items, filtered.Count));
    }

    public Task<MediaAsset?> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.MediaAssets.TryGetValue(mediaAssetId, out var mediaAsset) ? mediaAsset : null);
    }

    public Task<IReadOnlyList<MediaAsset>> GetByIdsAsync(IReadOnlyCollection<Guid> mediaAssetIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = mediaAssetIds
            .Distinct()
            .Where(id => _store.MediaAssets.ContainsKey(id))
            .Select(id => _store.MediaAssets[id])
            .ToList();

        return Task.FromResult<IReadOnlyList<MediaAsset>>(items);
    }

    public Task<MediaAsset?> GetByStorageKeyAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var mediaAsset = _store.MediaAssets.Values.FirstOrDefault(x => string.Equals(x.StorageKey, storageKey, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(mediaAsset);
    }

    public Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.MediaAssets[mediaAsset.Id] = mediaAsset;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<MediaAsset> ApplySorting(IEnumerable<MediaAsset> mediaAssets, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => mediaAssets.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.FileName),
            "updatedatutc" => mediaAssets.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.FileName),
            "-filename" => mediaAssets.OrderByDescending(x => x.FileName),
            _ => mediaAssets.OrderBy(x => x.FileName)
        };
    }
}
