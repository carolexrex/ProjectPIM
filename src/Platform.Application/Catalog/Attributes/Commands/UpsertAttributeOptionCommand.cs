namespace Platform.Application.Catalog.Attributes.Commands;

public sealed record UpsertAttributeOptionCommand(
    Guid? Id,
    string Code,
    string Value,
    int SortOrder);
