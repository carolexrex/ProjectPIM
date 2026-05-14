using Platform.Application.Customers;
using Platform.Application.Customers.Queries;
using Platform.Domain.Customers;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Customers;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryCustomerRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<CustomerListResult> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _store.Customers.Values
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Email.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || x.FirstName.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || x.LastName.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase))
            .Where(x => !query.IsGuest.HasValue || x.IsGuest == query.IsGuest.Value)
            .Where(x => !query.DefaultMarketId.HasValue || x.DefaultMarketId == query.DefaultMarketId.Value);

        filtered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => filtered.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Email),
            "updatedatutc" => filtered.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Email),
            "-lastname" => filtered.OrderByDescending(x => x.LastName).ThenBy(x => x.FirstName),
            "lastname" => filtered.OrderBy(x => x.LastName).ThenBy(x => x.FirstName),
            "-firstname" => filtered.OrderByDescending(x => x.FirstName).ThenBy(x => x.LastName),
            "firstname" => filtered.OrderBy(x => x.FirstName).ThenBy(x => x.LastName),
            "-email" => filtered.OrderByDescending(x => x.Email),
            _ => filtered.OrderBy(x => x.Email)
        };

        var materialized = filtered.ToList();
        return Task.FromResult(new CustomerListResult(materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(), materialized.Count));
    }

    public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        _store.Customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }

    public Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var customer = _store.Customers.Values.FirstOrDefault(x => string.Equals(x.NormalizedEmail, normalizedEmail, StringComparison.Ordinal));
        return Task.FromResult(customer);
    }

    public Task<IReadOnlyList<Customer>> GetByIdsAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Customer>>(_store.Customers.Values.Where(x => customerIds.Contains(x.Id)).ToList());
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _store.Customers[customer.Id] = customer;
        return Task.CompletedTask;
    }
}
