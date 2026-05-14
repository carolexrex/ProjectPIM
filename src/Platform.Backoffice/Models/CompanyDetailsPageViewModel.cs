using Platform.Contracts.Companies;

namespace Platform.Backoffice.Models;

public sealed class CompanyDetailsPageViewModel
{
    public CompanyUpdateViewModel Company { get; init; } = new();
    public IReadOnlyList<CompanyAddressDto> Addresses { get; init; } = [];
    public IReadOnlyList<CompanyMembershipUpdateViewModel> Memberships { get; init; } = [];
    public CompanyAddressCreateViewModel AddressForm { get; init; } = new();
    public CompanyMembershipCreateViewModel MembershipForm { get; init; } = new();
}
