using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Categories.Queries;
using Platform.Domain.Catalog.Categories;

namespace Platform.Infrastructure.Catalog.Categories;

public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryCategoryRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<CategoryListResult> ListAsync(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
                _store.Categories.Values
                    .Where(category => string.IsNullOrWhiteSpace(query.Search)
                        || category.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                        || category.Translations.Any(x => x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
                    .Where(category => string.IsNullOrWhiteSpace(query.Status)
                        || string.Equals(category.Status, query.Status, StringComparison.OrdinalIgnoreCase))
                    .Where(category => query.ParentCategoryId is null || category.ParentCategoryId == query.ParentCategoryId),
                query.Sort)
            .ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new CategoryListResult(items, filtered.Count));
    }

    public Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Categories.TryGetValue(categoryId, out var category) ? category : null);
    }

    public Task<IReadOnlyList<Category>> GetByIdsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Category> categories = categoryIds
            .Where(id => _store.Categories.ContainsKey(id))
            .Select(id => _store.Categories[id])
            .ToList();
        return Task.FromResult(categories);
    }

    public Task<Category?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var category = _store.Categories.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(category);
    }

    public Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Category> categories = _store.Categories.Values
            .Where(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(categories);
    }

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var category = _store.Categories.Values.FirstOrDefault(x =>
            string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase)
            && x.Translations.Any(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase)));
        return Task.FromResult(category);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Categories[category.Id] = category;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<Category> ApplySorting(IEnumerable<Category> categories, string? sort)
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
