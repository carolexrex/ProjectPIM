using Platform.Contracts.Catalog.Markets;

namespace Platform.Backoffice.Models;

public sealed class MarketListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<MarketSummaryDto> Markets { get; init; } = [];
    public int Total { get; init; }
}
