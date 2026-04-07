namespace Platform.Application.Catalog.Variants.Commands;

public sealed record UpdateVariantCommand(
    Guid VariantId,
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
    IReadOnlyList<CreateVariantAttributeValueCommand> AttributeValues,
    string RowVersion);
