namespace Platform.Application.Customers.Commands;

public sealed record AddCustomerAddressCommand(
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
