namespace Platform.Application.Catalog.Variants.Commands;

public sealed record CreateVariantAttributeValueCommand(
    Guid ProductAttributeId,
    Guid? AttributeOptionId,
    string? ValueText);

public sealed record CreateVariantCommand(
    Guid ProductId,
    string Sku,
    string? Ean,
    string? Mpn,
    string? Barcode,
    Guid ProductStatusDefinitionId,
    bool IsDefaultVariant,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    IReadOnlyList<CreateVariantAttributeValueCommand> AttributeValues);
