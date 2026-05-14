using Platform.Application.Customers.Commands;
using Platform.Application.Customers.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Customers;

namespace Platform.Application.Customers;

public interface ICustomerAdminApplicationService
{
    Task<PagedResponse<CustomerSummaryDto>> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken);
    Task<CustomerDetailsDto?> GetByIdAsync(GetCustomerByIdQuery query, CancellationToken cancellationToken);
    Task<CustomerDetailsDto> CreateAsync(CreateCustomerCommand command, CancellationToken cancellationToken);
    Task<CustomerDetailsDto?> UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken);
    Task<CustomerAddressDto?> AddAddressAsync(AddCustomerAddressCommand command, CancellationToken cancellationToken);
}
