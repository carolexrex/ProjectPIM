namespace Platform.Contracts.Catalog.Variants;

public sealed record VariantAttributeValueDto(
    Guid ProductAttributeId,
    Guid? AttributeOptionId,
    string? ValueText);

public sealed record VariantMediaDto(
    Guid Id,
    Guid MediaAssetId,
    string Type,
    int SortOrder,
    bool IsPrimary,
    string FileName,
    string PublicUrl,
    string? Title,
    string? AltText);

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
    IReadOnlyList<VariantMediaDto> Media,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record VariantLookupDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductNumber,
    string? ProductDefaultName);
