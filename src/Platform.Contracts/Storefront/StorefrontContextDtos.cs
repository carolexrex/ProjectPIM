namespace Platform.Contracts.Storefront;

public sealed record StorefrontChannelContextDto(
    Guid Id,
    string Code,
    string Name,
    string? HostName);

public sealed record StorefrontMarketContextDto(
    Guid Id,
    string Code,
    string Name,
    string DefaultCurrencyCode,
    string DefaultCultureCode,
    string PriceDisplayMode);

public sealed record StorefrontContextDto(
    StorefrontChannelContextDto? Channel,
    StorefrontMarketContextDto Market,
    string ActiveCultureCode,
    string ActiveCurrencyCode,
    IReadOnlyList<string> AvailableCultureCodes,
    IReadOnlyList<string> AvailableCurrencyCodes);
