using Platform.Contracts.Catalog.Brands;

namespace Platform.Backoffice.Models;

public sealed class BrandDetailsPageViewModel
{
    public BrandUpdateViewModel Brand { get; init; } = new();
    public IReadOnlyList<BrandTranslationDto> Translations { get; init; } = [];
    public BrandTranslationUpsertViewModel TranslationForm { get; init; } = new();
}
