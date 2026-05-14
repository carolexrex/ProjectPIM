namespace Platform.Contracts.Customers;

public sealed record CustomerAddressDto(
    Guid Id,
    Guid CustomerId,
    string Type,
    string? Attention,
    string FirstName,
    string LastName,
    string? CompanyName,
    string Line1,
    string? Line2,
    string PostalCode,
    string City,
    string? Region,
    string CountryCode,
    string? Phone,
    string? Email,
    bool IsDefault);

public sealed record CustomerSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    bool IsGuest,
    Guid? DefaultMarketId,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record CustomerDetailsDto(
    Guid Id,
    string? ExternalId,
    string? UserId,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? PreferredCulture,
    Guid? DefaultMarketId,
    string Status,
    bool IsGuest,
    IReadOnlyList<CustomerAddressDto> Addresses,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
