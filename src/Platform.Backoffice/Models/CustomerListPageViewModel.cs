using Platform.Contracts.Customers;

namespace Platform.Backoffice.Models;

public sealed class CustomerListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public bool? IsGuest { get; init; }
    public IReadOnlyList<CustomerSummaryDto> Customers { get; init; } = [];
    public int Total { get; init; }
}
