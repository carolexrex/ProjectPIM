namespace Platform.Application.Catalog.Brands.Commands;

public sealed record CreateBrandCommand(
    string Code,
    string? WebsiteUrl,
    Guid? LogoMediaAssetId,
    int SortOrder);
