namespace Platform.Application.Storefront;

public sealed record GetStorefrontCategoriesQuery(
    string? ChannelCode,
    string? MarketCode,
    string? CultureCode,
    string? CurrencyCode,
    string? HostName);
