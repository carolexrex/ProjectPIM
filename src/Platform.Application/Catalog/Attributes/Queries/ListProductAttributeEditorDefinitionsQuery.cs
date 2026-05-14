namespace Platform.Application.Catalog.Attributes.Queries;

public sealed record ListProductAttributeEditorDefinitionsQuery(
    string Scope,
    string? Status);
