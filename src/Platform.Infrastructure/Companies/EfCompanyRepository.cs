using Microsoft.EntityFrameworkCore;
using Platform.Application.Companies;
using Platform.Application.Companies.Queries;
using Platform.Domain.Companies;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Companies;

public sealed class EfCompanyRepository : ICompanyRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfCompanyRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompanyListResult> ListAsync(ListCompaniesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Companies
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Code.Contains(query.Search)
                || x.Name.Contains(query.Search)
                || (x.LegalName != null && x.LegalName.Contains(query.Search)))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => !query.DefaultMarketId.HasValue || x.DefaultMarketId == query.DefaultMarketId.Value);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Addresses)
            .Include(x => x.Memberships)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CompanyListResult(items, total);
    }

    public async Task<Company?> GetByIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return await _dbContext.Companies
            .Include(x => x.Addresses)
            .Include(x => x.Memberships)
            .FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken);
    }

    public async Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Companies
            .Include(x => x.Addresses)
            .Include(x => x.Memberships)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<Company?> GetByMembershipIdAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        return await _dbContext.Companies
            .Include(x => x.Addresses)
            .Include(x => x.Memberships)
            .FirstOrDefaultAsync(x => x.Memberships.Any(m => m.Id == membershipId), cancellationToken);
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        await _dbContext.Companies.AddAsync(company, cancellationToken);
    }

    private static IQueryable<Company> ApplySorting(IQueryable<Company> companies, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => companies.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => companies.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-name" => companies.OrderByDescending(x => x.Name).ThenBy(x => x.Code),
            "name" => companies.OrderBy(x => x.Name).ThenBy(x => x.Code),
            "-code" => companies.OrderByDescending(x => x.Code),
            _ => companies.OrderBy(x => x.Code)
        };
    }
}
