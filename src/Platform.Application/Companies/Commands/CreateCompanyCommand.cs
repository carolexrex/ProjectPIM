namespace Platform.Application.Companies.Commands;

public sealed record CreateCompanyCommand(
    string? ExternalId,
    string Code,
    string Name,
    string? LegalName,
    string? OrganizationNumber,
    string? VatNumber,
    string? Email,
    string? Phone,
    Guid? DefaultMarketId,
    string? DefaultCurrency,
    string Status);
