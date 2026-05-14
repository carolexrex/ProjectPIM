namespace Platform.Application.Storefront;

public sealed record GetStorefrontProductByProductNumberQuery(
    string ProductNumber,
    string? ChannelCode,
    string? MarketCode,
    string? CultureCode,
    string? CurrencyCode,
    string? HostName);
