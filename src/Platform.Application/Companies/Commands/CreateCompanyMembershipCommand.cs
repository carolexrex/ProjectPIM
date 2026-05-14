namespace Platform.Application.Companies.Commands;

public sealed record CreateCompanyMembershipCommand(
    Guid CompanyId,
    Guid CustomerId,
    string Role,
    string Status,
    bool IsDefaultCompany,
    bool CanPlaceOrders,
    bool CanApproveOrders,
    bool CanManageUsers,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc);
