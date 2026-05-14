namespace Platform.Application.Orders.Commands;

public sealed record CreateOrderCommand(
    Guid? CartId,
    string? CartRowVersion,
    Guid? CustomerId,
    Guid? CompanyId,
    Guid? MarketId,
    string? CurrencyCode,
    string? CultureCode,
    string? Email,
    IReadOnlyList<CreateOrderLineItem> Lines,
    IReadOnlyList<CreateOrderAddressItem> Addresses);

public sealed record CreateOrderLineItem(Guid VariantId, decimal Quantity, string? Comment);

public sealed record CreateOrderAddressItem(
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
