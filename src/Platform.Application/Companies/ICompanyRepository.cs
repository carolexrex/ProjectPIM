using Platform.Application.Companies.Queries;
using Platform.Domain.Companies;

namespace Platform.Application.Companies;

public interface ICompanyRepository
{
    Task<CompanyListResult> ListAsync(ListCompaniesQuery query, CancellationToken cancellationToken);
    Task<Company?> GetByIdAsync(Guid companyId, CancellationToken cancellationToken);
    Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<Company?> GetByMembershipIdAsync(Guid membershipId, CancellationToken cancellationToken);
    Task AddAsync(Company company, CancellationToken cancellationToken);
}
