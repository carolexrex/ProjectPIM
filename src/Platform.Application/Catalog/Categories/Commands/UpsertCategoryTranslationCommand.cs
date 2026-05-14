namespace Platform.Application.Catalog.Categories.Commands;

public sealed record UpsertCategoryTranslationCommand(
    Guid CategoryId,
    string CultureCode,
    string Name,
    string Slug,
    string? Description);
