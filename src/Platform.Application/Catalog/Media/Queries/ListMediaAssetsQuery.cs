namespace Platform.Application.Catalog.Media.Queries;

public sealed record ListMediaAssetsQuery(
    string? Search,
    string? Status,
    string? ContentType,
    int Page,
    int PageSize,
    string? Sort);
