namespace Platform.Application.Catalog.Brands.Queries;

public sealed record ListBrandsQuery(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 50,
    string? Sort = null);
