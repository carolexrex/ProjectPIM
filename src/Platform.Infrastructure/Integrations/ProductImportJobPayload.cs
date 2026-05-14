namespace Platform.Infrastructure.Integrations;

public sealed record ProductImportJobPayload(
    IReadOnlyList<ProductImportJobPayloadItem> Products);

public sealed record ProductImportJobPayloadItem(
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
    IReadOnlyList<ProductImportJobPayloadAttributeValue> AttributeValues,
    IReadOnlyList<ProductImportJobPayloadTranslation> Translations);

public sealed record ProductImportJobPayloadAttributeValue(
    string ProductAttributeCode,
    string? AttributeOptionCode,
    string? ValueText);

public sealed record ProductImportJobPayloadTranslation(
    string CultureCode,
    string Name,
    string? ShortDescription,
    string? LongDescription,
    string? SeoTitle,
    string? SeoDescription);
