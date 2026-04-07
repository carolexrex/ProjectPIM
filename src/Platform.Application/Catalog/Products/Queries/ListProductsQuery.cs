namespace Platform.Application.Catalog.Products.Queries;

public sealed record ListProductsQuery(
    string? Search,
    string? Status,
    string? ProductStatusCode,
    Guid? BrandId,
    bool? HasVariants,
    int Page,
    int PageSize,
    string? Sort);
