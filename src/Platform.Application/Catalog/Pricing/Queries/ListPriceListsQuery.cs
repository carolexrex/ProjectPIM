namespace Platform.Application.Catalog.Pricing.Queries;

public sealed record ListPriceListsQuery(
    string? Search,
    string? CurrencyCode,
    string? Status,
    Guid? MarketId,
    int Page = 1,
    int PageSize = 50,
    string? Sort = null);
