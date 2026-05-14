namespace Platform.Application.Companies.Commands;

public sealed record UpdateCompanyMembershipCommand(
    Guid MembershipId,
    string Role,
    string Status,
    bool IsDefaultCompany,
    bool CanPlaceOrders,
    bool CanApproveOrders,
    bool CanManageUsers,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string RowVersion);
