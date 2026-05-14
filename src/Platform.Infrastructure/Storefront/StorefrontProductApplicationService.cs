using System.Text.Json;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Categories;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Domain.Catalog.Brands;
using Platform.Domain.Catalog.Categories;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontProductApplicationService : IStorefrontProductApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBrandRepository _brandRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IStorefrontContextApplicationService _contextService;
    private readonly IStorefrontProductProjectionRepository _projectionRepository;
    private readonly IStorefrontProjectionRefreshService _projectionRefreshService;

    public StorefrontProductApplicationService(
        IBrandRepository brandRepository,
        ICategoryRepository categoryRepository,
        IStorefrontContextApplicationService contextService,
        IStorefrontProductProjectionRepository projectionRepository,
        IStorefrontProjectionRefreshService projectionRefreshService)
    {
        _brandRepository = brandRepository;
        _categoryRepository = categoryRepository;
        _contextService = contextService;
        _projectionRepository = projectionRepository;
        _projectionRefreshService = projectionRefreshService;
    }

    public async Task<StorefrontProductListResult> ListAsync(GetStorefrontProductsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 24 : Math.Min(query.PageSize, 100);
        var normalizedSort = NormalizeSort(query.Sort);

        if (normalizedSort is null)
        {
            return StorefrontProductListResult.Invalid(
                nameof(query.Sort),
                $"Unsupported sort '{query.Sort}'. Supported values: {string.Join(", ", SupportedSorts)}.");
        }

        var contextResult = await ResolveContextAsync(
            query.ChannelCode,
            query.MarketCode,
            query.CultureCode,
            query.CurrencyCode,
            query.HostName,
            cancellationToken);

        if (contextResult.Status != StorefrontContextResolutionStatus.Success || contextResult.Context is null)
        {
            return StorefrontProductListResult.FromContextFailure(contextResult);
        }

        Category? category = null;
        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            category = await _categoryRepository.GetBySlugAsync(query.CategorySlug, cancellationToken);
            if (category is null || !IsActive(category.Status))
            {
                return StorefrontProductListResult.NotFound(contextResult.Context, "Category", query.CategorySlug);
            }
        }

        Brand? brand = null;
        if (!string.IsNullOrWhiteSpace(query.BrandCode))
        {
            brand = await _brandRepository.GetByCodeAsync(query.BrandCode, cancellationToken);
            if (brand is null || !IsActive(brand.Status))
            {
                return StorefrontProductListResult.NotFound(contextResult.Context, "Brand", query.BrandCode);
            }
        }

        var projections = await LoadContextProjectionsAsync(contextResult.Context, cancellationToken);

        var filtered = ApplySharedFilters(
                projections,
                query.Query,
                query.BrandCode,
                query.CategorySlug)
            .ToList();

        var sorted = ApplySorting(filtered, normalizedSort).ToList();
        var paged = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapSummary)
            .ToList();

        var facets = await BuildFacetsAsync(
            projections,
            contextResult.Context.ActiveCultureCode,
            query.Query,
            brand,
            query.CategorySlug,
            cancellationToken);

        return StorefrontProductListResult.Success(
            new StorefrontProductListResponseDto(
                paged,
                sorted.Count,
                page,
                pageSize,
                new StorefrontProductAppliedFiltersDto(
                    query.CategorySlug,
                    query.BrandCode,
                    query.Query,
                    normalizedSort),
                facets),
            contextResult.Context);
    }

    public async Task<StorefrontProductDetailsResult> GetBySlugAsync(GetStorefrontProductBySlugQuery query, CancellationToken cancellationToken)
    {
        var contextResult = await ResolveContextAsync(
            query.ChannelCode,
            query.MarketCode,
            query.CultureCode,
            query.CurrencyCode,
            query.HostName,
            cancellationToken);

        if (contextResult.Status != StorefrontContextResolutionStatus.Success || contextResult.Context is null)
        {
            return StorefrontProductDetailsResult.FromContextFailure(contextResult);
        }

        var projection = await LoadBySlugAsync(contextResult.Context, query.Slug, cancellationToken);
        if (projection is null || !projection.IsVisible)
        {
            return StorefrontProductDetailsResult.NotFound(contextResult.Context, "Product", query.Slug);
        }

        return StorefrontProductDetailsResult.Success(MapDetails(projection), contextResult.Context);
    }

    public async Task<StorefrontProductDetailsResult> GetByProductNumberAsync(GetStorefrontProductByProductNumberQuery query, CancellationToken cancellationToken)
    {
        var contextResult = await ResolveContextAsync(
            query.ChannelCode,
            query.MarketCode,
            query.CultureCode,
            query.CurrencyCode,
            query.HostName,
            cancellationToken);

        if (contextResult.Status != StorefrontContextResolutionStatus.Success || contextResult.Context is null)
        {
            return StorefrontProductDetailsResult.FromContextFailure(contextResult);
        }

        var projection = await LoadByProductNumberAsync(contextResult.Context, query.ProductNumber, cancellationToken);
        if (projection is null || !projection.IsVisible)
        {
            return StorefrontProductDetailsResult.NotFound(contextResult.Context, "Product", query.ProductNumber);
        }

        return StorefrontProductDetailsResult.Success(MapDetails(projection), contextResult.Context);
    }

    private async Task<StorefrontContextResolutionResult> ResolveContextAsync(
        string? channelCode,
        string? marketCode,
        string? cultureCode,
        string? currencyCode,
        string? hostName,
        CancellationToken cancellationToken)
    {
        return await _contextService.GetContextAsync(
            new GetStorefrontContextQuery(channelCode, marketCode, cultureCode, currencyCode, hostName),
            cancellationToken);
    }

    private async Task<IReadOnlyList<StorefrontProductProjection>> LoadContextProjectionsAsync(
        StorefrontContextDto context,
        CancellationToken cancellationToken)
    {
        var projections = await _projectionRepository.ListByContextAsync(
            context.Market.Code,
            context.ActiveCultureCode,
            context.ActiveCurrencyCode,
            cancellationToken);

        if (projections.Count > 0)
        {
            return projections;
        }

        await _projectionRefreshService.RebuildAllAsync(cancellationToken);

        return await _projectionRepository.ListByContextAsync(
            context.Market.Code,
            context.ActiveCultureCode,
            context.ActiveCurrencyCode,
            cancellationToken);
    }

    private async Task<StorefrontProductProjection?> LoadBySlugAsync(
        StorefrontContextDto context,
        string slug,
        CancellationToken cancellationToken)
    {
        var projection = await _projectionRepository.GetBySlugAsync(
            context.Market.Code,
            context.ActiveCultureCode,
            context.ActiveCurrencyCode,
            slug,
            cancellationToken);

        if (projection is not null)
        {
            return projection;
        }

        await _projectionRefreshService.RebuildAllAsync(cancellationToken);

        return await _projectionRepository.GetBySlugAsync(
            context.Market.Code,
            context.ActiveCultureCode,
            context.ActiveCurrencyCode,
            slug,
            cancellationToken);
    }

    private async Task<StorefrontProductProjection?> LoadByProductNumberAsync(
        StorefrontContextDto context,
        string productNumber,
        CancellationToken cancellationToken)
    {
        var projection = await _projectionRepository.GetByProductNumberAsync(
            context.Market.Code,
            context.ActiveCultureCode,
            context.ActiveCurrencyCode,
            productNumber,
            cancellationToken);

        if (projection is not null)
        {
            return projection;
        }

        await _projectionRefreshService.RebuildAllAsync(cancellationToken);

        return await _projectionRepository.GetByProductNumberAsync(
            context.Market.Code,
            context.ActiveCultureCode,
            context.ActiveCurrencyCode,
            productNumber,
            cancellationToken);
    }

    private async Task<StorefrontProductFacetsDto> BuildFacetsAsync(
        IReadOnlyList<StorefrontProductProjection> projections,
        string cultureCode,
        string? query,
        Brand? selectedBrand,
        string? selectedCategorySlug,
        CancellationToken cancellationToken)
    {
        var activeCategories = await _categoryRepository.ListActiveAsync(cancellationToken);
        var categoryBySlug = activeCategories.ToDictionary(
            x => ResolveCategorySlug(x, cultureCode),
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var categoryFacetSource = ApplyQueryFilter(
                projections.Where(x => selectedBrand is null || string.Equals(x.BrandCode, selectedBrand.Code, StringComparison.OrdinalIgnoreCase)),
                query)
            .ToList();

        var categoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var projection in categoryFacetSource)
        {
            foreach (var slug in ParseStringList(projection.CategoryFilterSlugsJson))
            {
                categoryCounts[slug] = categoryCounts.GetValueOrDefault(slug) + 1;
            }
        }

        var categoryFacets = categoryCounts
            .Where(x => categoryBySlug.ContainsKey(x.Key))
            .Select(x =>
            {
                var category = categoryBySlug[x.Key];
                return new StorefrontCategoryFacetDto(
                    category.Id,
                    category.Code,
                    ResolveCategorySlug(category, cultureCode),
                    ResolveCategoryName(category, cultureCode),
                    x.Value);
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var brandFacetSource = ApplySharedFilters(
                projections,
                query,
                null,
                selectedCategorySlug)
            .ToList();

        var brandFacets = brandFacetSource
            .Where(x => !string.IsNullOrWhiteSpace(x.BrandCode))
            .GroupBy(x => x.BrandCode!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new StorefrontBrandFacetDto(
                    first.BrandId ?? Guid.Empty,
                    first.BrandCode!,
                    first.BrandName ?? first.BrandCode!,
                    first.BrandSlug,
                    group.Count());
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new StorefrontProductFacetsDto(categoryFacets, brandFacets, SupportedSorts);
    }

    private static IEnumerable<StorefrontProductProjection> ApplySharedFilters(
        IEnumerable<StorefrontProductProjection> projections,
        string? query,
        string? brandCode,
        string? categorySlug)
    {
        return ApplyQueryFilter(projections.Where(x =>
                x.IsVisible
                && (string.IsNullOrWhiteSpace(brandCode) || string.Equals(x.BrandCode, brandCode, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(categorySlug) || ParseStringList(x.CategoryFilterSlugsJson).Contains(categorySlug, StringComparer.OrdinalIgnoreCase))),
            query);
    }

    private static IEnumerable<StorefrontProductProjection> ApplyQueryFilter(
        IEnumerable<StorefrontProductProjection> projections,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return projections;
        }

        return projections.Where(x =>
            x.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<StorefrontProductProjection> ApplySorting(
        IEnumerable<StorefrontProductProjection> projections,
        string sort)
    {
        return sort switch
        {
            "-name" => projections.OrderByDescending(x => x.SortName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SortProductNumber, StringComparer.OrdinalIgnoreCase),
            "name" => projections.OrderBy(x => x.SortName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SortProductNumber, StringComparer.OrdinalIgnoreCase),
            "-productnumber" => projections.OrderByDescending(x => x.SortProductNumber, StringComparer.OrdinalIgnoreCase),
            _ => projections.OrderBy(x => x.SortProductNumber, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static StorefrontProductSummaryDto MapSummary(StorefrontProductProjection projection)
    {
        return new StorefrontProductSummaryDto(
            projection.ProductId,
            projection.ProductNumber,
            projection.Slug,
            projection.Name,
            projection.ShortDescription,
            MapBrand(projection),
            projection.PrimaryImageUrl,
            projection.HasVariants,
            MapPrice(projection),
            MapAvailability(projection),
            MapBuyability(projection));
    }

    private static StorefrontProductDetailsDto MapDetails(StorefrontProductProjection projection)
    {
        return new StorefrontProductDetailsDto(
            projection.ProductId,
            projection.ProductNumber,
            projection.Slug,
            projection.ProductType,
            projection.Name,
            projection.ShortDescription,
            projection.LongDescription,
            projection.SeoTitle,
            projection.SeoDescription,
            MapBrand(projection),
            ParseJsonList<StorefrontProductCategoryReferenceDto>(projection.CategoriesJson),
            ParseJsonList<StorefrontProductMediaDto>(projection.MediaJson),
            ParseJsonList<StorefrontProductAttributeValueDto>(projection.AttributesJson),
            ParseJsonList<StorefrontProductVariantDto>(projection.VariantsJson),
            MapPrice(projection),
            MapAvailability(projection),
            MapBuyability(projection));
    }

    private static StorefrontBrandReferenceDto? MapBrand(StorefrontProductProjection projection)
    {
        if (string.IsNullOrWhiteSpace(projection.BrandCode))
        {
            return null;
        }

        return new StorefrontBrandReferenceDto(
            projection.BrandId ?? Guid.Empty,
            projection.BrandCode!,
            projection.BrandName ?? projection.BrandCode!,
            projection.BrandSlug,
            projection.BrandWebsiteUrl,
            projection.BrandLogoUrl);
    }

    private static StorefrontProductPriceDto? MapPrice(StorefrontProductProjection projection)
    {
        return projection.PriceAmount is decimal amount
            ? new StorefrontProductPriceDto(
                projection.CurrencyCode,
                amount,
                projection.CompareAtAmount,
                projection.VatIncluded ?? false,
                projection.PriceListCode ?? string.Empty)
            : null;
    }

    private static StorefrontAvailabilityDto MapAvailability(StorefrontProductProjection projection)
    {
        return new StorefrontAvailabilityDto(
            projection.AvailabilityStatus,
            projection.AvailableQuantity,
            projection.IsBackorderable);
    }

    private static StorefrontBuyabilityDto MapBuyability(StorefrontProductProjection projection)
    {
        return new StorefrontBuyabilityDto(
            projection.IsVisible,
            projection.IsBuyable,
            ParseStringList(projection.BuyabilityReasonsJson));
    }

    private static IReadOnlyList<T> ParseJsonList<T>(string json)
    {
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
    }

    private static IReadOnlyList<string> ParseStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static string ResolveCategoryName(Category category, string cultureCode)
    {
        var translation = category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault();

        return translation?.Name ?? category.Code;
    }

    private static string ResolveCategorySlug(Category category, string cultureCode)
    {
        var translation = category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault();

        return translation?.Slug ?? category.Code;
    }

    private static bool IsActive(string status)
    {
        return string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return "productnumber";
        }

        var normalized = sort.Trim().ToLowerInvariant();
        return SupportedSorts.Contains(normalized, StringComparer.Ordinal)
            ? normalized
            : null;
    }

    private static readonly IReadOnlyList<string> SupportedSorts =
    [
        "productnumber",
        "-productnumber",
        "name",
        "-name"
    ];

}
