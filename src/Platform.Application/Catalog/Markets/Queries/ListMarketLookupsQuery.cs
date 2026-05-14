namespace Platform.Application.Catalog.Markets.Queries;

public sealed record ListMarketLookupsQuery(
    string? Search,
    string? Status,
    string? CurrencyCode);
