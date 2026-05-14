using Microsoft.EntityFrameworkCore;
using Platform.Application.Customers;
using Platform.Application.Customers.Queries;
using Platform.Domain.Customers;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Customers;

public sealed class EfCustomerRepository : ICustomerRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfCustomerRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerListResult> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Customers
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Email.Contains(query.Search)
                || x.FirstName.Contains(query.Search)
                || x.LastName.Contains(query.Search))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status)
            .Where(x => !query.IsGuest.HasValue || x.IsGuest == query.IsGuest.Value)
            .Where(x => !query.DefaultMarketId.HasValue || x.DefaultMarketId == query.DefaultMarketId.Value);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Addresses)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CustomerListResult(items, total);
    }

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Customers
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Id == customerId, cancellationToken);
    }

    public async Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await _dbContext.Customers
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetByIdsAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Customers
            .AsNoTracking()
            .Include(x => x.Addresses)
            .Where(x => customerIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        await _dbContext.Customers.AddAsync(customer, cancellationToken);
    }

    private static IQueryable<Customer> ApplySorting(IQueryable<Customer> customers, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => customers.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Email),
            "updatedatutc" => customers.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Email),
            "-lastname" => customers.OrderByDescending(x => x.LastName).ThenBy(x => x.FirstName),
            "lastname" => customers.OrderBy(x => x.LastName).ThenBy(x => x.FirstName),
            "-firstname" => customers.OrderByDescending(x => x.FirstName).ThenBy(x => x.LastName),
            "firstname" => customers.OrderBy(x => x.FirstName).ThenBy(x => x.LastName),
            "-email" => customers.OrderByDescending(x => x.Email),
            _ => customers.OrderBy(x => x.Email)
        };
    }
}
