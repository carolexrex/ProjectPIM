namespace Platform.Application.Companies.Commands;

public sealed record AddCompanyAddressCommand(
    Guid CompanyId,
    string Type,
    string? Attention,
    string Line1,
    string? Line2,
    string PostalCode,
    string City,
    string? Region,
    string CountryCode,
    string? Email,
    string? Phone,
    bool IsDefault);
