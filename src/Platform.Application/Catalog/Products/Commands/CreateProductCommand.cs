namespace Platform.Application.Catalog.Products.Commands;

public sealed record CreateProductCommand(
    string ProductType,
    string ProductNumber,
    string Slug,
    Guid? BrandId,
    Guid ProductStatusDefinitionId,
    string TaxCategoryCode,
    string UnitOfMeasure,
    bool HasVariants,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<CreateProductAttributeValueCommand> AttributeValues,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height);
