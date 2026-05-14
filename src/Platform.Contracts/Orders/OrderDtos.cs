namespace Platform.Contracts.Orders;

public sealed record OrderLineDto(
    Guid Id,
    Guid VariantId,
    string Sku,
    string ProductName,
    string? VariantDescription,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);

public sealed record OrderAddressDto(
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

public sealed record OrderStatusHistoryDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string? Comment);

public sealed record PaymentTransactionDto(
    Guid Id,
    string Provider,
    string ProviderReference,
    string Type,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string Status,
    Guid? CustomerId,
    Guid? CompanyId,
    Guid MarketId,
    string CurrencyCode,
    string Email,
    decimal GrandTotal,
    string PaymentStatus,
    string FulfillmentStatus,
    DateTime PlacedAtUtc,
    string RowVersion);

public sealed record OrderDetailsDto(
    Guid Id,
    Guid? SourceCartId,
    string OrderNumber,
    string Status,
    Guid? CustomerId,
    Guid? CompanyId,
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
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<OrderAddressDto> Addresses,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory,
    IReadOnlyList<PaymentTransactionDto> PaymentTransactions,
    string RowVersion);
