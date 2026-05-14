using Platform.Contracts.Catalog.Pricing;

namespace Platform.Backoffice.Models;

public sealed class PriceListListPageViewModel
{
    public string? Search { get; init; }
    public string? CurrencyCode { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<PriceListSummaryDto> PriceLists { get; init; } = [];
    public int Total { get; init; }
}
