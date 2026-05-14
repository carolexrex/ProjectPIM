namespace Platform.Application.Catalog.Products.Queries;

public sealed record ListProductLookupsQuery(
    string? Search,
    string? Status,
    bool? HasVariants,
    Guid? ExcludedProductId);
