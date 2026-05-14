using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Media.Queries;
using Platform.Domain.Catalog.Media;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Media;

public sealed class EfMediaAssetRepository : IMediaAssetRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfMediaAssetRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MediaAssetListResult> ListAsync(ListMediaAssetsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.MediaAssets
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.FileName.Contains(query.Search)
                || x.PublicUrl.Contains(query.Search)
                || x.Title != null && x.Title.Contains(query.Search))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => string.IsNullOrWhiteSpace(query.ContentType) || x.ContentType == query.ContentType);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new MediaAssetListResult(items, total);
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        return await _dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == mediaAssetId, cancellationToken);
    }

    public async Task<IReadOnlyList<MediaAsset>> GetByIdsAsync(IReadOnlyCollection<Guid> mediaAssetIds, CancellationToken cancellationToken)
    {
        if (mediaAssetIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.MediaAssets
            .Where(x => mediaAssetIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<MediaAsset?> GetByStorageKeyAsync(string storageKey, CancellationToken cancellationToken)
    {
        return await _dbContext.MediaAssets.FirstOrDefaultAsync(x => x.StorageKey == storageKey, cancellationToken);
    }

    public async Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        await _dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
    }

    private static IQueryable<MediaAsset> ApplySorting(IQueryable<MediaAsset> mediaAssets, string? sort)
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
