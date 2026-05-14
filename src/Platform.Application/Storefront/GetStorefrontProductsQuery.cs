namespace Platform.Application.Storefront;

public sealed record GetStorefrontProductsQuery(
    string? ChannelCode,
    string? MarketCode,
    string? CultureCode,
    string? CurrencyCode,
    string? HostName,
    string? CategorySlug,
    string? BrandCode,
    string? Query,
    string? Sort,
    int Page,
    int PageSize);
