namespace Platform.Application.Integrations.Commands;

public sealed record CreateBrandImportJobCommand(
    IReadOnlyList<BrandImportJobItemInput> Brands);

public sealed record BrandImportJobItemInput(
    string Code,
    string? WebsiteUrl,
    Guid? LogoMediaAssetId,
    int SortOrder,
    IReadOnlyList<BrandImportJobTranslationInput> Translations);

public sealed record BrandImportJobTranslationInput(
    string CultureCode,
    string Name,
    string Slug,
    string? Description);
