using System.Text.Json;
using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Variants;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Domain.Catalog.Attributes;
using Platform.Domain.Catalog.Brands;
using Platform.Domain.Catalog.Categories;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Media;
using Platform.Domain.Catalog.Pricing;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontProjectionBuilder : IStorefrontProjectionBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBrandRepository _brandRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IInventoryBalanceRepository _inventoryBalanceRepository;
    private readonly IInventoryLocationRepository _inventoryLocationRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IProductAttributeRepository _productAttributeRepository;
    private readonly IProductRepository _productRepository;
    private readonly IVariantRepository _variantRepository;

    public StorefrontProjectionBuilder(
        IBrandRepository brandRepository,
        ICategoryRepository categoryRepository,
        IInventoryBalanceRepository inventoryBalanceRepository,
        IInventoryLocationRepository inventoryLocationRepository,
        IMarketRepository marketRepository,
        IMediaAssetRepository mediaAssetRepository,
        IPriceListRepository priceListRepository,
        IProductAttributeRepository productAttributeRepository,
        IProductRepository productRepository,
        IVariantRepository variantRepository)
    {
        _brandRepository = brandRepository;
        _categoryRepository = categoryRepository;
        _inventoryBalanceRepository = inventoryBalanceRepository;
        _inventoryLocationRepository = inventoryLocationRepository;
        _marketRepository = marketRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _priceListRepository = priceListRepository;
        _productAttributeRepository = productAttributeRepository;
        _productRepository = productRepository;
        _variantRepository = variantRepository;
    }

    public async Task<IReadOnlyList<StorefrontProductProjection>> BuildForProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return [];
        }

        var markets = await _marketRepository.ListActiveAsync(cancellationToken);
        var visibleMarkets = markets
            .Where(market => IsVisibleInMarket(product, market))
            .ToList();

        if (visibleMarkets.Count == 0)
        {
            return [];
        }

        var categories = await _categoryRepository.GetByIdsAsync(
            product.CategoryAssignments.Select(x => x.CategoryId).Distinct().ToList(),
            cancellationToken);
        var allActiveCategories = await _categoryRepository.ListActiveAsync(cancellationToken);
        var brand = product.BrandId.HasValue
            ? await _brandRepository.GetByIdAsync(product.BrandId.Value, cancellationToken)
            : null;
        var variants = await _variantRepository.ListByProductAsync(product.Id, cancellationToken);

        var mediaAssetIds = product.Media
            .Select(x => x.MediaAssetId)
            .Concat(variants.SelectMany(x => x.Media.Select(media => media.MediaAssetId)))
            .Concat(brand?.LogoMediaAssetId is Guid logoId ? [logoId] : [])
            .Distinct()
            .ToList();

        var mediaAssets = await _mediaAssetRepository.GetByIdsAsync(mediaAssetIds, cancellationToken);
        var mediaById = mediaAssets.ToDictionary(x => x.Id);

        var attributeIds = product.AttributeValues
            .Select(x => x.ProductAttributeId)
            .Concat(variants.SelectMany(x => x.AttributeValues.Select(value => value.ProductAttributeId)))
            .Distinct()
            .ToList();
        var attributeDefinitions = await _productAttributeRepository.GetByIdsAsync(attributeIds, cancellationToken);
        var attributeById = attributeDefinitions.ToDictionary(x => x.Id);

        var variantBalances = new Dictionary<Guid, IReadOnlyList<InventoryBalance>>();
        var inventoryLocationIds = new HashSet<Guid>();
        foreach (var variant in variants)
        {
            var balances = await _inventoryBalanceRepository.ListByVariantAsync(variant.Id, cancellationToken);
            variantBalances[variant.Id] = balances;

            foreach (var balance in balances)
            {
                inventoryLocationIds.Add(balance.InventoryLocationId);
            }
        }

        var locations = await _inventoryLocationRepository.GetByIdsAsync(inventoryLocationIds.ToList(), cancellationToken);
        var projectedAtUtc = DateTime.UtcNow;
        var projections = new List<StorefrontProductProjection>();

        foreach (var market in visibleMarkets)
        {
            var marketLocationIds = locations
                .Where(location => IsActive(location.Status) && location.MarketAssignments.Any(x => x.MarketId == market.Id))
                .Select(location => location.Id)
                .ToHashSet();

            var availabilityByVariantId = variants.ToDictionary(
                variant => variant.Id,
                variant => ResolveAvailability(variantBalances.GetValueOrDefault(variant.Id) ?? [], marketLocationIds));

            foreach (var currency in market.Currencies.Select(x => x.CurrencyCode))
            {
                var priceLists = await _priceListRepository.ListActiveByMarketAsync(
                    market.Id,
                    currency,
                    projectedAtUtc,
                    cancellationToken);

                var priceByVariantId = variants.ToDictionary(
                    variant => variant.Id,
                    variant => ResolvePrice(variant.Id, priceLists));

                foreach (var culture in market.Cultures.Select(x => x.CultureCode))
                {
                    var translation = ResolveTranslation(product, culture);
                    var brandTranslation = brand is null ? null : ResolveTranslation(brand, culture);
                    var categoryNodes = categories
                        .Where(x => IsActive(x.Status))
                        .OrderBy(x => x.SortOrder)
                        .ThenBy(x => ResolveCategoryName(x, culture), StringComparer.OrdinalIgnoreCase)
                        .Select(x => new StorefrontProductCategoryReferenceDto(
                            x.Id,
                            x.Code,
                            ResolveCategorySlug(x, culture),
                            ResolveCategoryName(x, culture)))
                        .ToList();
                    var categoryFilterSlugs = BuildCategoryFilterSlugs(allActiveCategories, product.CategoryAssignments.Select(x => x.CategoryId), culture);

                    var productMedia = MapMedia(product.Media, mediaById);
                    var productAttributes = MapAttributeValues(product.AttributeValues, attributeById);

                    var variantRows = variants
                        .OrderByDescending(x => x.IsDefaultVariant)
                        .ThenBy(x => x.Sku, StringComparer.OrdinalIgnoreCase)
                        .Select(variant =>
                        {
                            var media = MapMedia(variant.Media, mediaById);
                            var availability = availabilityByVariantId[variant.Id];
                            var price = priceByVariantId[variant.Id];
                            var buyability = ResolveVariantBuyability(product, market, variant, price, availability);

                            return new StorefrontProductVariantDto(
                                variant.Id,
                                variant.Sku,
                                variant.Ean,
                                variant.Mpn,
                                variant.Barcode,
                                variant.IsDefaultVariant,
                                ResolvePrimaryImageUrl(media, variant.PrimaryImageUrl),
                                media,
                                MapAttributeValues(variant.AttributeValues, attributeById),
                                price,
                                availability,
                                buyability);
                        })
                        .ToList();

                    var topPrice = variantRows
                        .Where(x => x.Buyability.IsVisible && x.Price is not null)
                        .OrderBy(x => x.Price!.Amount)
                        .Select(x => x.Price)
                        .FirstOrDefault();
                    var productAvailability = ResolveAggregateAvailability(variantRows);
                    var productBuyability = ResolveProductBuyability(product, market, variantRows, topPrice, productAvailability);
                    var searchText = BuildSearchText(
                        product,
                        translation?.Name,
                        brandTranslation?.Name,
                        categoryNodes,
                        variants);

                    projections.Add(new StorefrontProductProjection(
                        Guid.NewGuid(),
                        product.Id,
                        market.Id,
                        market.Code,
                        culture,
                        currency,
                        product.ProductNumber,
                        product.Slug,
                        product.ProductType,
                        translation?.Name ?? product.ProductNumber,
                        translation?.ShortDescription,
                        translation?.LongDescription,
                        translation?.SeoTitle,
                        translation?.SeoDescription,
                        brand?.Id,
                        brand?.Code,
                        brandTranslation?.Name ?? brand?.Code,
                        brandTranslation?.Slug,
                        brand?.WebsiteUrl,
                        ResolveBrandLogoUrl(brand, mediaById),
                        JsonSerializer.Serialize(categoryNodes.Select(x => x.Code).ToList(), JsonOptions),
                        JsonSerializer.Serialize(categoryNodes.Select(x => x.Slug).ToList(), JsonOptions),
                        JsonSerializer.Serialize(categoryNodes.Select(x => x.Name).ToList(), JsonOptions),
                        JsonSerializer.Serialize(categoryFilterSlugs, JsonOptions),
                        JsonSerializer.Serialize(categoryNodes, JsonOptions),
                        ResolvePrimaryImageUrl(productMedia, product.PrimaryImageUrl) ?? variantRows.FirstOrDefault(x => x.IsDefaultVariant)?.PrimaryImageUrl,
                        JsonSerializer.Serialize(productAttributes, JsonOptions),
                        JsonSerializer.Serialize(productMedia, JsonOptions),
                        product.HasVariants,
                        productBuyability.IsVisible,
                        productBuyability.IsBuyable,
                        JsonSerializer.Serialize(productBuyability.Reasons, JsonOptions),
                        productAvailability.Status,
                        productAvailability.AvailableQuantity,
                        productAvailability.IsBackorderable,
                        topPrice?.Amount,
                        topPrice?.CompareAtAmount,
                        topPrice?.VatIncluded,
                        topPrice?.PriceListCode,
                        JsonSerializer.Serialize(variantRows, JsonOptions),
                        searchText,
                        NormalizeSortName(translation?.Name ?? product.ProductNumber),
                        product.ProductNumber,
                        topPrice?.Amount,
                        brandTranslation?.Name,
                        ResolveSourceUpdatedAtUtc(product, brand, allActiveCategories, variants, priceLists, variantBalances, locations),
                        projectedAtUtc));
                }
            }
        }

        return projections;
    }

    private static string BuildSearchText(
        Product product,
        string? localizedName,
        string? brandName,
        IReadOnlyList<StorefrontProductCategoryReferenceDto> categories,
        IReadOnlyList<Variant> variants)
    {
        var parts = new List<string?>
        {
            product.ProductNumber,
            product.Slug,
            localizedName,
            brandName
        };

        parts.AddRange(categories.Select(x => x.Name));
        parts.AddRange(categories.Select(x => x.Slug));
        parts.AddRange(variants.Select(x => x.Sku));
        parts.AddRange(variants.Select(x => x.Ean));
        parts.AddRange(variants.Select(x => x.Mpn));
        parts.AddRange(variants.Select(x => x.Barcode));

        return string.Join(
            " ",
            parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
    }

    private static DateTime ResolveSourceUpdatedAtUtc(
        Product product,
        Brand? brand,
        IReadOnlyList<Category> categories,
        IReadOnlyList<Variant> variants,
        IReadOnlyList<PriceList> priceLists,
        IReadOnlyDictionary<Guid, IReadOnlyList<InventoryBalance>> balances,
        IReadOnlyList<InventoryLocation> locations)
    {
        var timestamps = new List<DateTime>
        {
            product.UpdatedAtUtc
        };

        if (brand is not null)
        {
            timestamps.Add(brand.UpdatedAtUtc);
        }

        timestamps.AddRange(categories.Select(x => x.UpdatedAtUtc));
        timestamps.AddRange(variants.Select(x => x.UpdatedAtUtc));
        timestamps.AddRange(priceLists.Select(x => x.UpdatedAtUtc));
        timestamps.AddRange(balances.Values.SelectMany(x => x).Select(x => x.UpdatedAtUtc));
        timestamps.AddRange(locations.Select(x => x.UpdatedAtUtc));

        return timestamps.Max();
    }

    private static StorefrontProductPriceDto? ResolvePrice(Guid variantId, IReadOnlyList<PriceList> priceLists)
    {
        foreach (var priceList in priceLists)
        {
            var entry = priceList.Entries
                .Where(x => string.Equals(x.TargetType, "Variant", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.TargetId == variantId)
                .Where(x => x.MinQuantity <= 1)
                .OrderByDescending(x => x.MinQuantity)
                .ThenByDescending(x => x.ValidFromUtc)
                .FirstOrDefault();

            if (entry is null)
            {
                continue;
            }

            return new StorefrontProductPriceDto(
                priceList.CurrencyCode,
                entry.Amount,
                entry.CompareAtAmount,
                priceList.VatIncluded,
                priceList.Code);
        }

        return null;
    }

    private static StorefrontAvailabilityDto ResolveAvailability(
        IReadOnlyList<InventoryBalance> balances,
        IReadOnlySet<Guid> marketLocationIds)
    {
        var relevantBalances = balances
            .Where(x => marketLocationIds.Contains(x.InventoryLocationId))
            .ToList();

        if (relevantBalances.Count == 0)
        {
            return new StorefrontAvailabilityDto("Unavailable", 0m, false);
        }

        var availableQuantity = relevantBalances.Sum(x => Math.Max(x.AvailableQuantity, 0m));
        var isBackorderable = relevantBalances.Any(x => x.Backorderable);
        var status = availableQuantity > 0m
            ? "InStock"
            : isBackorderable
                ? "Backorderable"
                : "OutOfStock";

        return new StorefrontAvailabilityDto(status, availableQuantity, isBackorderable);
    }

    private static StorefrontAvailabilityDto ResolveAggregateAvailability(IReadOnlyList<StorefrontProductVariantDto> variants)
    {
        var visibleVariants = variants.Where(x => x.Buyability.IsVisible).ToList();
        if (visibleVariants.Count == 0)
        {
            return new StorefrontAvailabilityDto("Unavailable", 0m, false);
        }

        var availableQuantity = visibleVariants.Sum(x => Math.Max(x.Availability.AvailableQuantity, 0m));
        var isBackorderable = visibleVariants.Any(x => x.Availability.IsBackorderable);
        var status = availableQuantity > 0m
            ? "InStock"
            : isBackorderable
                ? "Backorderable"
                : visibleVariants.Any(x => !string.Equals(x.Availability.Status, "Unavailable", StringComparison.OrdinalIgnoreCase))
                    ? "OutOfStock"
                    : "Unavailable";

        return new StorefrontAvailabilityDto(status, availableQuantity, isBackorderable);
    }

    private static StorefrontBuyabilityDto ResolveProductBuyability(
        Product product,
        Market market,
        IReadOnlyList<StorefrontProductVariantDto> variants,
        StorefrontProductPriceDto? price,
        StorefrontAvailabilityDto availability)
    {
        var reasons = new List<string>();
        var isVisible = IsVisibleInMarket(product, market);

        if (!isVisible)
        {
            reasons.Add("NotVisibleInMarket");
        }

        if (!product.ProductStatus.IsBuyable)
        {
            reasons.Add("ProductStatusNotBuyable");
        }

        if (variants.Count == 0)
        {
            reasons.Add("NoVariants");
        }

        if (!variants.Any(x => x.Buyability.IsBuyable))
        {
            reasons.Add("NoBuyableVariants");
        }

        if (price is null)
        {
            reasons.Add("MissingPrice");
        }

        if (string.Equals(availability.Status, "OutOfStock", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("OutOfStock");
        }
        else if (string.Equals(availability.Status, "Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Unavailable");
        }

        return new StorefrontBuyabilityDto(
            isVisible,
            isVisible
            && product.ProductStatus.IsBuyable
            && price is not null
            && variants.Any(x => x.Buyability.IsBuyable)
            && !string.Equals(availability.Status, "OutOfStock", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(availability.Status, "Unavailable", StringComparison.OrdinalIgnoreCase),
            reasons.Distinct(StringComparer.Ordinal).ToList());
    }

    private static StorefrontBuyabilityDto ResolveVariantBuyability(
        Product product,
        Market market,
        Variant variant,
        StorefrontProductPriceDto? price,
        StorefrontAvailabilityDto availability)
    {
        var reasons = new List<string>();
        var isVisible = IsVisibleInMarket(product, market) && IsActive(variant.Status);

        if (!isVisible)
        {
            reasons.Add("NotVisibleInMarket");
        }

        if (!variant.ProductStatus.IsBuyable)
        {
            reasons.Add("VariantStatusNotBuyable");
        }

        if (price is null)
        {
            reasons.Add("MissingPrice");
        }

        if (string.Equals(availability.Status, "OutOfStock", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("OutOfStock");
        }
        else if (string.Equals(availability.Status, "Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Unavailable");
        }

        return new StorefrontBuyabilityDto(
            isVisible,
            isVisible
            && variant.ProductStatus.IsBuyable
            && price is not null
            && !string.Equals(availability.Status, "OutOfStock", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(availability.Status, "Unavailable", StringComparison.OrdinalIgnoreCase),
            reasons.Distinct(StringComparer.Ordinal).ToList());
    }

    private static IReadOnlyList<StorefrontProductMediaDto> MapMedia(
        IEnumerable<ProductMedia> media,
        IReadOnlyDictionary<Guid, MediaAsset> mediaById)
    {
        return media
            .Where(x => mediaById.ContainsKey(x.MediaAssetId))
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => new StorefrontProductMediaDto(
                x.MediaAssetId,
                x.Type,
                mediaById[x.MediaAssetId].PublicUrl,
                mediaById[x.MediaAssetId].AltText,
                mediaById[x.MediaAssetId].Title,
                x.SortOrder,
                x.IsPrimary))
            .ToList();
    }

    private static IReadOnlyList<StorefrontProductMediaDto> MapMedia(
        IEnumerable<VariantMedia> media,
        IReadOnlyDictionary<Guid, MediaAsset> mediaById)
    {
        return media
            .Where(x => mediaById.ContainsKey(x.MediaAssetId))
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => new StorefrontProductMediaDto(
                x.MediaAssetId,
                x.Type,
                mediaById[x.MediaAssetId].PublicUrl,
                mediaById[x.MediaAssetId].AltText,
                mediaById[x.MediaAssetId].Title,
                x.SortOrder,
                x.IsPrimary))
            .ToList();
    }

    private static IReadOnlyList<StorefrontProductAttributeValueDto> MapAttributeValues(
        IEnumerable<ProductAttributeValue> attributeValues,
        IReadOnlyDictionary<Guid, ProductAttribute> definitions)
    {
        return attributeValues
            .Where(x => definitions.ContainsKey(x.ProductAttributeId))
            .OrderBy(x => definitions[x.ProductAttributeId].SortOrder)
            .ThenBy(x => definitions[x.ProductAttributeId].Code, StringComparer.OrdinalIgnoreCase)
            .Select(x => MapAttributeValue(x.ProductAttributeId, x.AttributeOptionId, x.ValueText, definitions[x.ProductAttributeId]))
            .ToList();
    }

    private static IReadOnlyList<StorefrontProductAttributeValueDto> MapAttributeValues(
        IEnumerable<VariantAttributeValue> attributeValues,
        IReadOnlyDictionary<Guid, ProductAttribute> definitions)
    {
        return attributeValues
            .Where(x => definitions.ContainsKey(x.ProductAttributeId))
            .OrderBy(x => definitions[x.ProductAttributeId].SortOrder)
            .ThenBy(x => definitions[x.ProductAttributeId].Code, StringComparer.OrdinalIgnoreCase)
            .Select(x => MapAttributeValue(x.ProductAttributeId, x.AttributeOptionId, x.ValueText, definitions[x.ProductAttributeId]))
            .ToList();
    }

    private static StorefrontProductAttributeValueDto MapAttributeValue(
        Guid attributeId,
        Guid? attributeOptionId,
        string? valueText,
        ProductAttribute definition)
    {
        var option = attributeOptionId.HasValue
            ? definition.Options.FirstOrDefault(x => x.Id == attributeOptionId.Value)
            : null;

        return new StorefrontProductAttributeValueDto(
            attributeId,
            definition.Code,
            definition.Name,
            option?.Code,
            option?.Value,
            valueText);
    }

    private static string? ResolvePrimaryImageUrl(IReadOnlyList<StorefrontProductMediaDto> media, string? fallbackUrl)
    {
        return media.FirstOrDefault(x => x.IsPrimary)?.Url
            ?? media.FirstOrDefault()?.Url
            ?? fallbackUrl;
    }

    private static string? ResolveBrandLogoUrl(Brand? brand, IReadOnlyDictionary<Guid, MediaAsset> mediaById)
    {
        return brand?.LogoMediaAssetId is Guid logoId && mediaById.TryGetValue(logoId, out var asset)
            ? asset.PublicUrl
            : null;
    }

    private static ProductTranslation? ResolveTranslation(Product product, string cultureCode)
    {
        return product.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? product.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? product.Translations.FirstOrDefault();
    }

    private static CategoryTranslation? ResolveTranslation(Category category, string cultureCode)
    {
        return category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? category.Translations.FirstOrDefault();
    }

    private static BrandTranslation? ResolveTranslation(Brand brand, string cultureCode)
    {
        return brand.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))
            ?? brand.Translations.FirstOrDefault(x =>
                string.Equals(x.CultureCode, "en-GB", StringComparison.OrdinalIgnoreCase))
            ?? brand.Translations.FirstOrDefault();
    }

    private static string ResolveCategoryName(Category category, string cultureCode)
    {
        return ResolveTranslation(category, cultureCode)?.Name ?? category.Code;
    }

    private static string ResolveCategorySlug(Category category, string cultureCode)
    {
        return ResolveTranslation(category, cultureCode)?.Slug ?? category.Code;
    }

    private static string NormalizeSortName(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static IReadOnlyList<string> BuildCategoryFilterSlugs(
        IReadOnlyList<Category> categories,
        IEnumerable<Guid> assignedCategoryIds,
        string cultureCode)
    {
        var byId = categories.ToDictionary(x => x.Id);
        var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var categoryId in assignedCategoryIds.Distinct())
        {
            if (!byId.TryGetValue(categoryId, out var current))
            {
                continue;
            }

            while (true)
            {
                slugs.Add(ResolveCategorySlug(current, cultureCode));

                if (!current.ParentCategoryId.HasValue || !byId.TryGetValue(current.ParentCategoryId.Value, out current))
                {
                    break;
                }
            }
        }

        return slugs.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsVisibleInMarket(Product product, Market market)
    {
        return IsActive(product.Status)
            && market.ProductAssignments.Any(x =>
                x.ProductId == product.Id
                && string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsActive(string status)
    {
        return string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
    }
}
