using Platform.Contracts.Catalog.Brands;

namespace Platform.Backoffice.Models;

public sealed class BrandListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<BrandSummaryDto> Brands { get; init; } = [];
    public int Total { get; init; }
}
