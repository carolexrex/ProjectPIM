namespace Platform.Contracts.Catalog.Products;

public sealed record ProductStatusDto(
    Guid Id,
    string Code,
    string Name,
    bool IsBuyable);

public sealed record ProductTranslationDto(
    string CultureCode,
    string Name,
    string? ShortDescription,
    string? LongDescription,
    string? SeoTitle,
    string? SeoDescription);

public sealed record ProductSummaryDto(
    Guid Id,
    string ProductNumber,
    string Slug,
    string ProductType,
    string Status,
    ProductStatusDto ProductStatus,
    string? BrandName,
    string? DefaultName,
    string? PrimaryImageUrl,
    bool HasVariants,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record ProductDetailsDto(
    Guid Id,
    string ProductNumber,
    string Slug,
    string ProductType,
    string Status,
    ProductStatusDto ProductStatus,
    Guid? BrandId,
    string? BrandName,
    string TaxCategoryCode,
    string UnitOfMeasure,
    string? PrimaryImageUrl,
    bool HasVariants,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    IReadOnlyList<ProductTranslationDto> Translations,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
