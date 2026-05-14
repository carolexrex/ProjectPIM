namespace Platform.Contracts.Catalog.Categories;

public sealed record CategoryTranslationDto(
    string CultureCode,
    string Name,
    string Slug,
    string? Description);

public sealed record CategorySummaryDto(
    Guid Id,
    string Code,
    string? DefaultName,
    Guid? ParentCategoryId,
    int SortOrder,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record CategoryDetailsDto(
    Guid Id,
    string Code,
    Guid? ParentCategoryId,
    int SortOrder,
    string Status,
    IReadOnlyList<CategoryTranslationDto> Translations,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
