using Platform.Application.Companies.Commands;
using Platform.Application.Companies.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Companies;

namespace Platform.Application.Companies;

public interface ICompanyAdminApplicationService
{
    Task<PagedResponse<CompanySummaryDto>> ListAsync(ListCompaniesQuery query, CancellationToken cancellationToken);
    Task<CompanyDetailsDto?> GetByIdAsync(GetCompanyByIdQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyMembershipDto>?> ListMembershipsAsync(ListCompanyMembershipsQuery query, CancellationToken cancellationToken);
    Task<CompanyDetailsDto> CreateAsync(CreateCompanyCommand command, CancellationToken cancellationToken);
    Task<CompanyDetailsDto?> UpdateAsync(UpdateCompanyCommand command, CancellationToken cancellationToken);
    Task<CompanyAddressDto?> AddAddressAsync(AddCompanyAddressCommand command, CancellationToken cancellationToken);
    Task<CompanyMembershipDto?> CreateMembershipAsync(CreateCompanyMembershipCommand command, CancellationToken cancellationToken);
    Task<CompanyMembershipDto?> UpdateMembershipAsync(UpdateCompanyMembershipCommand command, CancellationToken cancellationToken);
}
