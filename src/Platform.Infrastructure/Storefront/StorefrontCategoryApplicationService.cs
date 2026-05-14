using Platform.Application.Catalog.Categories;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Domain.Catalog.Categories;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontCategoryApplicationService : IStorefrontCategoryApplicationService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IStorefrontContextApplicationService _contextService;

    public StorefrontCategoryApplicationService(
        ICategoryRepository categoryRepository,
        IStorefrontContextApplicationService contextService)
    {
        _categoryRepository = categoryRepository;
        _contextService = contextService;
    }

    public async Task<StorefrontCategoryListResult> ListAsync(GetStorefrontCategoriesQuery query, CancellationToken cancellationToken)
    {
        var contextResult = await _contextService.GetContextAsync(
            new GetStorefrontContextQuery(
                query.ChannelCode,
                query.MarketCode,
                query.CultureCode,
                query.CurrencyCode,
                query.HostName),
            cancellationToken);

        if (contextResult.Status != StorefrontContextResolutionStatus.Success || contextResult.Context is null)
        {
            return StorefrontCategoryListResult.FromContextFailure(contextResult);
        }

        var categories = await _categoryRepository.ListActiveAsync(cancellationToken);
        var nodes = BuildCategoryTree(categories, contextResult.Context.ActiveCultureCode);
        return StorefrontCategoryListResult.Success(nodes, contextResult.Context);
    }

    public async Task<StorefrontCategoryDetailsResult> GetBySlugAsync(GetStorefrontCategoryBySlugQuery query, CancellationToken cancellationToken)
    {
        var contextResult = await _contextService.GetContextAsync(
            new GetStorefrontContextQuery(
                query.ChannelCode,
                query.MarketCode,
                query.CultureCode,
                query.CurrencyCode,
                query.HostName),
            cancellationToken);

        if (contextResult.Status != StorefrontContextResolutionStatus.Success || contextResult.Context is null)
        {
            return StorefrontCategoryDetailsResult.FromContextFailure(contextResult);
        }

        var category = await _categoryRepository.GetBySlugAsync(query.Slug, cancellationToken);
        if (category is null || !IsActive(category.Status))
        {
            return StorefrontCategoryDetailsResult.NotFound(contextResult.Context, "Category", query.Slug);
        }

        var categories = await _categoryRepository.ListActiveAsync(cancellationToken);
        var byId = categories.ToDictionary(x => x.Id);
        var activeCultureCode = contextResult.Context.ActiveCultureCode;
        var children = categories
            .Where(x => x.ParentCategoryId == category.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => ResolveName(x, activeCultureCode))
            .Select(x => MapNode(x, categories, activeCultureCode))
            .ToList();

        var details = new StorefrontCategoryDetailsDto(
            category.Id,
            category.Code,
            ResolveSlug(category, activeCultureCode),
            ResolveName(category, activeCultureCode),
            ResolveDescription(category, activeCultureCode),
            category.ParentCategoryId,
            category.SortOrder,
            BuildBreadcrumbs(category, byId, activeCultureCode),
            children);

        return StorefrontCategoryDetailsResult.Success(details, contextResult.Context);
    }

    private static IReadOnlyList<StorefrontCategoryNodeDto> BuildCategoryTree(
        IReadOnlyList<Category> categories,
        string cultureCode)
    {
        return categories
            .Where(x => x.ParentCategoryId is null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => ResolveName(x, cultureCode))
            .Select(x => MapNode(x, categories, cultureCode))
            .ToList();
    }

    private static StorefrontCategoryNodeDto MapNode(
        Category category,
        IReadOnlyList<Category> allCategories,
        string cultureCode)
    {
        var children = allCategories
            .Where(x => x.ParentCategoryId == category.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => ResolveName(x, cultureCode))
            .Select(x => MapNode(x, allCategories, cultureCode))
            .ToList();

        return new StorefrontCategoryNodeDto(
            category.Id,
            category.Code,
            ResolveSlug(category, cultureCode),
            ResolveName(category, cultureCode),
            ResolveDescription(category, cultureCode),
            category.ParentCategoryId,
            category.SortOrder,
            children);
    }

    private static IReadOnlyList<StorefrontCategoryBreadcrumbDto> BuildBreadcrumbs(
        Category category,
        IReadOnlyDictionary<Guid, Category> byId,
        string cultureCode)
    {
        var breadcrumbs = new List<StorefrontCategoryBreadcrumbDto>();
        Category? current = category;

        while (current is not null)
        {
            breadcrumbs.Add(new StorefrontCategoryBreadcrumbDto(
                current.Id,
                current.Code,
                ResolveSlug(current, cultureCode),
                ResolveName(current, cultureCode)));

            current = current.ParentCategoryId is Guid parentId && byId.TryGetValue(parentId, out var parent)
                ? parent
                : null;
        }

        breadcrumbs.Reverse();
        return breadcrumbs;
    }

    private static string ResolveName(Category category, string cultureCode)
    {
        var translation = category.Translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault();

        return translation?.Name ?? category.Code;
    }

    private static string ResolveSlug(Category category, string cultureCode)
    {
        var translation = category.Translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault();

        return translation?.Slug ?? category.Code;
    }

    private static string? ResolveDescription(Category category, string cultureCode)
    {
        var translation = category.Translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault();

        return translation?.Description;
    }

    private static bool IsActive(string status)
    {
        return string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
    }
}
