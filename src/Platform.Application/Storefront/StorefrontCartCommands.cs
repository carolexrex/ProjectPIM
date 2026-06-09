namespace Platform.Application.Storefront;

public sealed record CreateStorefrontCartCommand(
    string? ChannelCode,
    string? MarketCode,
    string? CultureCode,
    string? CurrencyCode,
    string? HostName,
    string? Email,
    IReadOnlyList<CreateStorefrontCartLineItem> Lines,
    IReadOnlyList<CreateStorefrontCartAddressItem> Addresses);

public sealed record CreateStorefrontCartLineItem(
    Guid VariantId,
    decimal Quantity,
    string? Comment);

public sealed record CreateStorefrontCartAddressItem(
    string Type,
    string FirstName,
    string LastName,
    string? CompanyName,
    string Line1,
    string? Line2,
    string PostalCode,
    string City,
    string? Region,
    string CountryCode,
    string? Email,
    string? Phone);

public sealed record GetStorefrontCartByIdQuery(
    Guid CartId,
    string? CartAccessToken);

public sealed record RepriceStorefrontCartCommand(
    Guid CartId,
    string RowVersion,
    string? CartAccessToken);

public sealed record CheckoutStorefrontCartCommand(
    Guid CartId,
    string RowVersion,
    string? CartAccessToken);
