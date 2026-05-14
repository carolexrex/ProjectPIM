using Platform.Contracts.Customers;

namespace Platform.Backoffice.Models;

public sealed class CustomerDetailsPageViewModel
{
    public CustomerUpdateViewModel Customer { get; init; } = new();
    public IReadOnlyList<CustomerAddressDto> Addresses { get; init; } = [];
    public CustomerAddressCreateViewModel AddressForm { get; init; } = new();
}
