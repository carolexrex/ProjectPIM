using Platform.Application.Customers.Queries;
using Platform.Domain.Customers;

namespace Platform.Application.Customers;

public interface ICustomerRepository
{
    Task<CustomerListResult> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<IReadOnlyList<Customer>> GetByIdsAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken);
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
}
