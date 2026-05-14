using Platform.Contracts.Companies;

namespace Platform.Backoffice.Models;

public sealed class CompanyListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<CompanySummaryDto> Companies { get; init; } = [];
    public int Total { get; init; }
}
