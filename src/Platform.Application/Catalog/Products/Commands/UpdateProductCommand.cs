namespace Platform.Application.Catalog.Products.Commands;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string ProductType,
    string Slug,
    Guid? BrandId,
    Guid ProductStatusDefinitionId,
    string TaxCategoryCode,
    string UnitOfMeasure,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<CreateProductAttributeValueCommand> AttributeValues,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string RowVersion);
