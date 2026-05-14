namespace Platform.Contracts.Catalog.Attributes;

public sealed record AttributeOptionDto(
    Guid Id,
    string Code,
    string Value,
    int SortOrder);

public sealed record ProductAttributeSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Scope,
    string DataType,
    bool IsVariantDefining,
    bool IsFilterable,
    bool IsRequired,
    int OptionCount,
    string Status,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record ProductAttributeDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string Scope,
    string DataType,
    bool IsVariantDefining,
    bool IsFilterable,
    bool IsRequired,
    int SortOrder,
    string Status,
    IReadOnlyList<AttributeOptionDto> Options,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record ProductAttributeEditorDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    string Scope,
    string DataType,
    bool IsRequired,
    IReadOnlyList<AttributeOptionDto> Options);
