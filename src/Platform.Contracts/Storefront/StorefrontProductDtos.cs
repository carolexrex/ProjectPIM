namespace Platform.Contracts.Storefront;

public sealed record StorefrontProductAppliedFiltersDto(
    string? CategorySlug,
    string? BrandCode,
    string? Query,
    string Sort);

public sealed record StorefrontCategoryFacetDto(
    Guid Id,
    string Code,
    string Slug,
    string Name,
    int Count);

public sealed record StorefrontBrandFacetDto(
    Guid Id,
    string Code,
    string Name,
    string? Slug,
    int Count);

public sealed record StorefrontProductFacetsDto(
    IReadOnlyList<StorefrontCategoryFacetDto> Categories,
    IReadOnlyList<StorefrontBrandFacetDto> Brands,
    IReadOnlyList<string> SortOptions);

public sealed record StorefrontProductListResponseDto(
    IReadOnlyList<StorefrontProductSummaryDto> Items,
    int Total,
    int Page,
    int PageSize,
    StorefrontProductAppliedFiltersDto AppliedFilters,
    StorefrontProductFacetsDto Facets);

public sealed record StorefrontBrandReferenceDto(
    Guid Id,
    string Code,
    string Name,
    string? Slug,
    string? WebsiteUrl,
    string? LogoUrl);

public sealed record StorefrontProductCategoryReferenceDto(
    Guid Id,
    string Code,
    string Slug,
    string Name);

public sealed record StorefrontProductMediaDto(
    Guid Id,
    string Type,
    string Url,
    string? AltText,
    string? Title,
    int SortOrder,
    bool IsPrimary);

public sealed record StorefrontProductAttributeValueDto(
    Guid AttributeId,
    string AttributeCode,
    string AttributeName,
    string? OptionCode,
    string? OptionLabel,
    string? ValueText);

public sealed record StorefrontProductPriceDto(
    string CurrencyCode,
    decimal Amount,
    decimal? CompareAtAmount,
    bool VatIncluded,
    string PriceListCode);

public sealed record StorefrontAvailabilityDto(
    string Status,
    decimal AvailableQuantity,
    bool IsBackorderable);

public sealed record StorefrontBuyabilityDto(
    bool IsVisible,
    bool IsBuyable,
    IReadOnlyList<string> Reasons);

public sealed record StorefrontProductSummaryDto(
    Guid Id,
    string ProductNumber,
    string Slug,
    string Name,
    string? ShortDescription,
    StorefrontBrandReferenceDto? Brand,
    string? PrimaryImageUrl,
    bool HasVariants,
    StorefrontProductPriceDto? Price,
    StorefrontAvailabilityDto Availability,
    StorefrontBuyabilityDto Buyability);

public sealed record StorefrontProductVariantDto(
    Guid Id,
    string Sku,
    string? Ean,
    string? Mpn,
    string? Barcode,
    bool IsDefaultVariant,
    string? PrimaryImageUrl,
    IReadOnlyList<StorefrontProductMediaDto> Media,
    IReadOnlyList<StorefrontProductAttributeValueDto> Attributes,
    StorefrontProductPriceDto? Price,
    StorefrontAvailabilityDto Availability,
    StorefrontBuyabilityDto Buyability);

public sealed record StorefrontProductDetailsDto(
    Guid Id,
    string ProductNumber,
    string Slug,
    string ProductType,
    string Name,
    string? ShortDescription,
    string? LongDescription,
    string? SeoTitle,
    string? SeoDescription,
    StorefrontBrandReferenceDto? Brand,
    IReadOnlyList<StorefrontProductCategoryReferenceDto> Categories,
    IReadOnlyList<StorefrontProductMediaDto> Media,
    IReadOnlyList<StorefrontProductAttributeValueDto> Attributes,
    IReadOnlyList<StorefrontProductVariantDto> Variants,
    StorefrontProductPriceDto? Price,
    StorefrontAvailabilityDto Availability,
    StorefrontBuyabilityDto Buyability);
