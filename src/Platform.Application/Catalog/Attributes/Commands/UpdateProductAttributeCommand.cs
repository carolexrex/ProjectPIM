namespace Platform.Application.Catalog.Attributes.Commands;

public sealed record UpdateProductAttributeCommand(
    Guid AttributeId,
    string Code,
    string Name,
    string Scope,
    string DataType,
    bool IsVariantDefining,
    bool IsFilterable,
    bool IsRequired,
    int SortOrder,
    string RowVersion,
    IReadOnlyList<UpsertAttributeOptionCommand> Options);
