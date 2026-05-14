using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Categories.Queries;
using Platform.Domain.Catalog.Categories;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Categories;

public sealed class EfCategoryRepository : ICategoryRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfCategoryRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CategoryListResult> ListAsync(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Categories
            .AsNoTracking()
            .Where(category => string.IsNullOrWhiteSpace(query.Search)
                || category.Code.Contains(query.Search)
                || category.Translations.Any(x => x.Name.Contains(query.Search)))
            .Where(category => string.IsNullOrWhiteSpace(query.Status) || category.Status == query.Status)
            .Where(category => query.ParentCategoryId == null || category.ParentCategoryId == query.ParentCategoryId);

        var total = await filteredQuery.CountAsync(cancellationToken);

        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Translations)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CategoryListResult(items, total);
    }

    public async Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => categoryIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.Status == "Active")
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x =>
                x.Status == "Active"
                && x.Translations.Any(t => t.Slug == slug), cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        await _dbContext.Categories.AddAsync(category, cancellationToken);
    }

    private static IQueryable<Category> ApplySorting(IQueryable<Category> categories, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => categories.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => categories.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-sortorder" => categories.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Code),
            "sortorder" => categories.OrderBy(x => x.SortOrder).ThenBy(x => x.Code),
            "-code" => categories.OrderByDescending(x => x.Code),
            _ => categories.OrderBy(x => x.Code)
        };
    }
}
