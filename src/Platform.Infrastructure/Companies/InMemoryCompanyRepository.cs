using Platform.Application.Companies;
using Platform.Application.Companies.Queries;
using Platform.Domain.Companies;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Companies;

public sealed class InMemoryCompanyRepository : ICompanyRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryCompanyRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<CompanyListResult> ListAsync(ListCompaniesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _store.Companies.Values
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(x.LegalName) && x.LegalName.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(x => !query.DefaultMarketId.HasValue || x.DefaultMarketId == query.DefaultMarketId.Value);

        filtered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => filtered.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => filtered.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-name" => filtered.OrderByDescending(x => x.Name).ThenBy(x => x.Code),
            "name" => filtered.OrderBy(x => x.Name).ThenBy(x => x.Code),
            "-code" => filtered.OrderByDescending(x => x.Code),
            _ => filtered.OrderBy(x => x.Code)
        };

        var materialized = filtered.ToList();
        return Task.FromResult(new CompanyListResult(materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(), materialized.Count));
    }

    public Task<Company?> GetByIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        _store.Companies.TryGetValue(companyId, out var company);
        return Task.FromResult(company);
    }

    public Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var company = _store.Companies.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(company);
    }

    public Task<Company?> GetByMembershipIdAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        var company = _store.Companies.Values.FirstOrDefault(x => x.Memberships.Any(m => m.Id == membershipId));
        return Task.FromResult(company);
    }

    public Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        _store.Companies[company.Id] = company;
        return Task.CompletedTask;
    }
}
