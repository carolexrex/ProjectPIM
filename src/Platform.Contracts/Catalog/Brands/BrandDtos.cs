namespace Platform.Contracts.Catalog.Brands;

public sealed record BrandTranslationDto(
    string CultureCode,
    string Name,
    string Slug,
    string? Description);

public sealed record BrandSummaryDto(
    Guid Id,
    string Code,
    string? DefaultName,
    string? WebsiteUrl,
    Guid? LogoMediaAssetId,
    string? LogoPublicUrl,
    int SortOrder,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record BrandDetailsDto(
    Guid Id,
    string Code,
    string? WebsiteUrl,
    Guid? LogoMediaAssetId,
    string? LogoFileName,
    string? LogoPublicUrl,
    int SortOrder,
    string Status,
    IReadOnlyList<BrandTranslationDto> Translations,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
