using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Brands.Queries;
using Platform.Domain.Catalog.Brands;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Brands;

public sealed class EfBrandRepository : IBrandRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfBrandRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BrandListResult> ListAsync(ListBrandsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Brands
            .AsNoTracking()
            .Where(brand => string.IsNullOrWhiteSpace(query.Search)
                || brand.Code.Contains(query.Search)
                || brand.Translations.Any(x => x.Name.Contains(query.Search)))
            .Where(brand => string.IsNullOrWhiteSpace(query.Status) || brand.Status == query.Status);

        var total = await filteredQuery.CountAsync(cancellationToken);

        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Translations)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new BrandListResult(items, total);
    }

    public async Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == brandId, cancellationToken);
    }

    public async Task<IReadOnlyList<Brand>> ListForExportAsync(string? search, string? status, CancellationToken cancellationToken)
    {
        return await ApplySorting(
                _dbContext.Brands
                    .AsNoTracking()
                    .Include(x => x.Translations)
                    .Where(brand => string.IsNullOrWhiteSpace(search)
                        || brand.Code.Contains(search)
                        || brand.Translations.Any(x => x.Name.Contains(search)))
                    .Where(brand => string.IsNullOrWhiteSpace(status) || brand.Status == status),
                "code")
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Brand>> GetByIdsAsync(IReadOnlyCollection<Guid> brandIds, CancellationToken cancellationToken)
    {
        if (brandIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Brands
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => brandIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        await _dbContext.Brands.AddAsync(brand, cancellationToken);
    }

    private static IQueryable<Brand> ApplySorting(IQueryable<Brand> brands, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => brands.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => brands.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-sortorder" => brands.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Code),
            "sortorder" => brands.OrderBy(x => x.SortOrder).ThenBy(x => x.Code),
            "-code" => brands.OrderByDescending(x => x.Code),
            _ => brands.OrderBy(x => x.Code)
        };
    }
}
