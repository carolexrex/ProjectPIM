namespace Platform.Application.Integrations.Commands;

public sealed record CreateProductImportJobCommand(
    IReadOnlyList<ProductImportJobItemInput> Products);

public sealed record ProductImportJobItemInput(
    string ProductType,
    string ProductNumber,
    string Slug,
    string? BrandCode,
    string ProductStatusCode,
    string TaxCategoryCode,
    string UnitOfMeasure,
    bool HasVariants,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    IReadOnlyList<string> CategoryCodes,
    IReadOnlyList<ProductImportJobAttributeValueInput> AttributeValues,
    IReadOnlyList<ProductImportJobTranslationInput> Translations);

public sealed record ProductImportJobAttributeValueInput(
    string ProductAttributeCode,
    string? AttributeOptionCode,
    string? ValueText);

public sealed record ProductImportJobTranslationInput(
    string CultureCode,
    string Name,
    string? ShortDescription,
    string? LongDescription,
    string? SeoTitle,
    string? SeoDescription);
