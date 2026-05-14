using Platform.Domain.Customers;

namespace Platform.Application.Customers;

public sealed record CustomerListResult(IReadOnlyList<Customer> Items, int Total);
