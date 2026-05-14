namespace Platform.Contracts.Companies;

public sealed record CompanyAddressDto(
    Guid Id,
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

public sealed record CompanyMembershipDto(
    Guid Id,
    Guid CompanyId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerDisplayName,
    string Role,
    string Status,
    bool IsDefaultCompany,
    bool CanPlaceOrders,
    bool CanApproveOrders,
    bool CanManageUsers,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    bool IsCurrentlyValid,
    string RowVersion);

public sealed record CompanySummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    Guid? DefaultMarketId,
    string? DefaultCurrency,
    int MembershipCount,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record CompanyDetailsDto(
    Guid Id,
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
    string Status,
    IReadOnlyList<CompanyAddressDto> Addresses,
    IReadOnlyList<CompanyMembershipDto> Memberships,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
