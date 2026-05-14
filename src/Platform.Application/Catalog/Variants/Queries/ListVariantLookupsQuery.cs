namespace Platform.Application.Catalog.Variants.Queries;

public sealed record ListVariantLookupsQuery(
    string? Search,
    string? Status,
    Guid? ProductId);
