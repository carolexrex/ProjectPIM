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

public sealed record ProductCategoryAssignmentDto(
    Guid CategoryId,
    string Code,
    string? Name);

public sealed record ProductAttributeValueDto(
    Guid ProductAttributeId,
    Guid? AttributeOptionId,
    string? ValueText);

public sealed record ProductRelationDto(
    Guid Id,
    Guid TargetProductId,
    string TargetProductNumber,
    string? TargetProductName,
    string RelationType,
    decimal? Quantity,
    int SortOrder);

public sealed record ProductMediaDto(
    Guid Id,
    Guid MediaAssetId,
    string Type,
    int SortOrder,
    bool IsPrimary,
    string FileName,
    string PublicUrl,
    string? Title,
    string? AltText);

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
    IReadOnlyList<ProductCategoryAssignmentDto> Categories,
    IReadOnlyList<ProductAttributeValueDto> AttributeValues,
    IReadOnlyList<ProductMediaDto> Media,
    IReadOnlyList<ProductRelationDto> Relations,
    IReadOnlyList<ProductTranslationDto> Translations,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record ProductLookupDto(
    Guid Id,
    string ProductNumber,
    string? DefaultName,
    bool HasVariants);
