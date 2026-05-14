using Platform.Contracts.Catalog.Categories;

namespace Platform.Backoffice.Models;

public sealed class CategoryDetailsPageViewModel
{
    public CategoryUpdateViewModel Category { get; init; } = new();
    public IReadOnlyList<CategoryTranslationDto> Translations { get; init; } = [];
    public CategoryTranslationUpsertViewModel TranslationForm { get; init; } = new();
}
