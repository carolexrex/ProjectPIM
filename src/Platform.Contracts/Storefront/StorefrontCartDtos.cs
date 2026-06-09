namespace Platform.Contracts.Storefront;

public sealed record StorefrontCartLineDto(
    Guid Id,
    Guid VariantId,
    string Sku,
    string ProductName,
    string? VariantDescription,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal,
    string? Comment);

public sealed record StorefrontCartAddressDto(
    Guid Id,
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

public sealed record StorefrontCartDto(
    Guid Id,
    Guid MarketId,
    string CurrencyCode,
    string CultureCode,
    string? Email,
    string Status,
    decimal Subtotal,
    decimal VatTotal,
    decimal GrandTotal,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<StorefrontCartLineDto> Lines,
    IReadOnlyList<StorefrontCartAddressDto> Addresses,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion,
    string CartAccessToken);

public sealed record StorefrontOrderLineDto(
    Guid Id,
    Guid VariantId,
    string Sku,
    string ProductName,
    string? VariantDescription,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);

public sealed record StorefrontOrderAddressDto(
    Guid Id,
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

public sealed record StorefrontOrderDto(
    Guid Id,
    Guid SourceCartId,
    string OrderNumber,
    string Status,
    Guid MarketId,
    string CurrencyCode,
    string CultureCode,
    string Email,
    decimal Subtotal,
    decimal VatTotal,
    decimal GrandTotal,
    string PaymentStatus,
    string FulfillmentStatus,
    DateTime PlacedAtUtc,
    IReadOnlyList<StorefrontOrderLineDto> Lines,
    IReadOnlyList<StorefrontOrderAddressDto> Addresses,
    string RowVersion);
