using Platform.Contracts.Catalog.Variants;

namespace Platform.Backoffice.Models;

public sealed class ProductDetailsPageViewModel
{
    public ProductUpdateViewModel Product { get; init; } = new();
    public IReadOnlyList<Platform.Contracts.Catalog.Products.ProductCategoryAssignmentDto> Categories { get; init; } = [];
    public IReadOnlyList<Platform.Contracts.Catalog.Products.ProductMediaDto> Media { get; init; } = [];
    public IReadOnlyList<Platform.Contracts.Catalog.Products.ProductRelationDto> Relations { get; init; } = [];
    public ProductMediaCreateViewModel MediaForm { get; init; } = new();
    public IReadOnlyList<Platform.Contracts.Catalog.Products.ProductTranslationDto> Translations { get; init; } = [];
    public ProductRelationCreateViewModel RelationForm { get; init; } = new();
    public ProductTranslationUpsertViewModel TranslationForm { get; init; } = new();
    public VariantCreateViewModel NewVariant { get; init; } = new();
    public IReadOnlyList<VariantSummaryDto> Variants { get; init; } = [];
}
