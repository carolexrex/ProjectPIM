namespace Platform.Infrastructure.Integrations;

public sealed record ProductExportJobResult(
    DateTime ExportedAtUtc,
    int TotalCount,
    IReadOnlyList<ProductExportJobResultItem> Items);

public sealed record ProductExportJobResultItem(
    Guid Id,
    string ProductNumber,
    string Slug,
    string ProductType,
    string Status,
    ProductExportJobStatusResult ProductStatus,
    Guid? BrandId,
    string? BrandCode,
    string? BrandName,
    bool HasVariants,
    string TaxCategoryCode,
    string UnitOfMeasure,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ProductExportJobTranslationResult> Translations,
    IReadOnlyList<ProductExportJobCategoryResult> Categories,
    IReadOnlyList<ProductExportJobAttributeValueResult> AttributeValues,
    IReadOnlyList<ProductExportJobMediaResult> Media,
    IReadOnlyList<ProductExportJobRelationResult> Relations);

public sealed record ProductExportJobStatusResult(
    Guid Id,
    string Code,
    string Name,
    bool IsBuyable);

public sealed record ProductExportJobTranslationResult(
    string CultureCode,
    string Name,
    string? ShortDescription,
    string? LongDescription,
    string? SeoTitle,
    string? SeoDescription);

public sealed record ProductExportJobCategoryResult(
    Guid CategoryId,
    string Code,
    string? Name);

public sealed record ProductExportJobAttributeValueResult(
    Guid ProductAttributeId,
    string? ProductAttributeCode,
    Guid? AttributeOptionId,
    string? AttributeOptionCode,
    string? ValueText);

public sealed record ProductExportJobMediaResult(
    Guid ProductMediaId,
    Guid MediaAssetId,
    string Type,
    int SortOrder,
    bool IsPrimary,
    string? FileName,
    string? PublicUrl,
    string? Title,
    string? AltText);

public sealed record ProductExportJobRelationResult(
    Guid RelationId,
    Guid TargetProductId,
    string? TargetProductNumber,
    string? TargetProductName,
    string RelationType,
    decimal? Quantity,
    int SortOrder);
