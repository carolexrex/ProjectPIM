namespace Platform.Contracts.Cart;

public sealed record CartLineDto(
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

public sealed record CartAddressDto(
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

public sealed record CartSummaryDto(
    Guid Id,
    Guid? CustomerId,
    Guid? CompanyId,
    Guid MarketId,
    string CurrencyCode,
    string CultureCode,
    string? Email,
    string Status,
    decimal GrandTotal,
    int LineCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record CartDetailsDto(
    Guid Id,
    Guid? CustomerId,
    Guid? CompanyId,
    Guid MarketId,
    string CurrencyCode,
    string CultureCode,
    string? Email,
    string Status,
    decimal Subtotal,
    decimal VatTotal,
    decimal GrandTotal,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<CartLineDto> Lines,
    IReadOnlyList<CartAddressDto> Addresses,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
