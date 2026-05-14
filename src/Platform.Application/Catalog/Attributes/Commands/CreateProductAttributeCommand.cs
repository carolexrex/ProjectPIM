namespace Platform.Application.Catalog.Attributes.Commands;

public sealed record CreateProductAttributeCommand(
    string Code,
    string Name,
    string Scope,
    string DataType,
    bool IsVariantDefining,
    bool IsFilterable,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<UpsertAttributeOptionCommand> Options);
