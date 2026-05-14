namespace Platform.Application.Catalog.Markets.Queries;

public sealed record ListMarketsQuery(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 50,
    string? Sort = null);
