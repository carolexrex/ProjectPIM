namespace Platform.Application.Storefront;

public sealed record GetStorefrontContextQuery(
    string? ChannelCode,
    string? MarketCode,
    string? CultureCode,
    string? CurrencyCode,
    string? HostName);
