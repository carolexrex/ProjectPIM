namespace Platform.Application.Catalog.Products.Commands;

public sealed record CreateProductAttributeValueCommand(
    Guid ProductAttributeId,
    Guid? AttributeOptionId,
    string? ValueText);
