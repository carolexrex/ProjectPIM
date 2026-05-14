namespace Platform.Application.Storefront;

public sealed record GetStorefrontCategoryBySlugQuery(
    string Slug,
    string? ChannelCode,
    string? MarketCode,
    string? CultureCode,
    string? CurrencyCode,
    string? HostName);
