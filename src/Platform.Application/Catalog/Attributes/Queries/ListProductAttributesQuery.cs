namespace Platform.Application.Catalog.Attributes.Queries;

public sealed record ListProductAttributesQuery(
    string? Search,
    string? Status,
    string? Scope,
    string? DataType,
    int Page,
    int PageSize,
    string? Sort);
