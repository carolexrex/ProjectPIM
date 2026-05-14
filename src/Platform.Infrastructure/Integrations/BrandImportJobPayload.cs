namespace Platform.Infrastructure.Integrations;

public sealed record BrandImportJobPayload(
    IReadOnlyList<BrandImportJobPayloadItem> Brands);

public sealed record BrandImportJobPayloadItem(
    string Code,
    string? WebsiteUrl,
    Guid? LogoMediaAssetId,
    int SortOrder,
    IReadOnlyList<BrandImportJobPayloadTranslation> Translations);

public sealed record BrandImportJobPayloadTranslation(
    string CultureCode,
    string Name,
    string Slug,
    string? Description);
