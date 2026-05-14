using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Markets;
using Platform.Application.Customers;
using Platform.Application.Customers.Commands;
using Platform.Application.Customers.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Customers;
using Platform.Domain.Customers;

namespace Platform.Infrastructure.Customers;

public sealed class CustomerAdminApplicationService : ICustomerAdminApplicationService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerAdminApplicationService(
        ICustomerRepository customerRepository,
        IMarketRepository marketRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _marketRepository = marketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<CustomerSummaryDto>> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken)
    {
        var result = await _customerRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<CustomerSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<CustomerDetailsDto?> GetByIdAsync(GetCustomerByIdQuery query, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        return customer is null ? null : MapDetails(customer);
    }

    public async Task<CustomerDetailsDto> CreateAsync(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        await EnsureEmailUniqueAsync(command.Email, command.IsGuest, null, cancellationToken);
        await EnsureMarketExistsAsync(command.DefaultMarketId, cancellationToken);

        var now = DateTime.UtcNow;
        var customer = new Customer(
            Guid.NewGuid(),
            command.ExternalId,
            command.UserId,
            command.Email,
            command.FirstName,
            command.LastName,
            command.Phone,
            command.PreferredCulture,
            command.DefaultMarketId,
            command.Status,
            command.IsGuest,
            now,
            now);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(customer);
    }

    public async Task<CustomerDetailsDto?> UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        await EnsureEmailUniqueAsync(command.Email, command.IsGuest, command.CustomerId, cancellationToken);
        await EnsureMarketExistsAsync(command.DefaultMarketId, cancellationToken);

        customer.Update(
            command.ExternalId,
            command.UserId,
            command.Email,
            command.FirstName,
            command.LastName,
            command.Phone,
            command.PreferredCulture,
            command.DefaultMarketId,
            command.Status,
            command.IsGuest,
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(customer);
    }

    public async Task<CustomerAddressDto?> AddAddressAsync(AddCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var address = customer.AddAddress(
            command.Type,
            command.Attention,
            command.FirstName,
            command.LastName,
            command.CompanyName,
            command.Line1,
            command.Line2,
            command.PostalCode,
            command.City,
            command.Region,
            command.CountryCode,
            command.Phone,
            command.Email,
            command.IsDefault);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapAddress(address);
    }

    private async Task EnsureEmailUniqueAsync(string email, bool isGuest, Guid? currentCustomerId, CancellationToken cancellationToken)
    {
        if (isGuest)
        {
            return;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var existing = await _customerRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null && existing.Id != currentCustomerId && !existing.IsGuest)
        {
            throw new ConflictException("Customer email already exists.");
        }
    }

    private async Task EnsureMarketExistsAsync(Guid? marketId, CancellationToken cancellationToken)
    {
        if (!marketId.HasValue)
        {
            return;
        }

        if (await _marketRepository.GetByIdAsync(marketId.Value, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(CreateCustomerCommand.DefaultMarketId), "Unknown market.");
        }
    }

    private static CustomerSummaryDto MapSummary(Customer customer)
    {
        return new CustomerSummaryDto(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.Status,
            customer.IsGuest,
            customer.DefaultMarketId,
            customer.UpdatedAtUtc,
            customer.RowVersion);
    }

    private static CustomerDetailsDto MapDetails(Customer customer)
    {
        return new CustomerDetailsDto(
            customer.Id,
            customer.ExternalId,
            customer.UserId,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.Phone,
            customer.PreferredCulture,
            customer.DefaultMarketId,
            customer.Status,
            customer.IsGuest,
            customer.Addresses.Select(MapAddress).ToList(),
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc,
            customer.RowVersion);
    }

    private static CustomerAddressDto MapAddress(CustomerAddress address)
    {
        return new CustomerAddressDto(
            address.Id,
            address.CustomerId,
            address.Type,
            address.Attention,
            address.FirstName,
            address.LastName,
            address.CompanyName,
            address.Line1,
            address.Line2,
            address.PostalCode,
            address.City,
            address.Region,
            address.CountryCode,
            address.Phone,
            address.Email,
            address.IsDefault);
    }
}
