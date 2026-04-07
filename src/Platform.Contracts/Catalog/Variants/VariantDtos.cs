namespace Platform.Contracts.Catalog.Variants;

public sealed record VariantAttributeValueDto(
    Guid ProductAttributeId,
    Guid? AttributeOptionId,
    string? ValueText);

public sealed record VariantSummaryDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string? Ean,
    string? Mpn,
    string? Barcode,
    string Status,
    Platform.Contracts.Catalog.Products.ProductStatusDto ProductStatus,
    bool IsDefaultVariant,
    string? PrimaryImageUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record VariantDetailsDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string? Ean,
    string? Mpn,
    string? Barcode,
    string Status,
    Platform.Contracts.Catalog.Products.ProductStatusDto ProductStatus,
    bool IsDefaultVariant,
    string? PrimaryImageUrl,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    IReadOnlyList<VariantAttributeValueDto> AttributeValues,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
