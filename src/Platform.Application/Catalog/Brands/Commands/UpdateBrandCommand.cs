namespace Platform.Application.Catalog.Brands.Commands;

public sealed record UpdateBrandCommand(
    Guid BrandId,
    string Code,
    string? WebsiteUrl,
    Guid? LogoMediaAssetId,
    int SortOrder,
    string RowVersion);
