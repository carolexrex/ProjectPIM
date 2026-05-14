namespace Platform.Application.Catalog.Brands.Commands;

public sealed record UpsertBrandTranslationCommand(
    Guid BrandId,
    string CultureCode,
    string Name,
    string Slug,
    string? Description);
