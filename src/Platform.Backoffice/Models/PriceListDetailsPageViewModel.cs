using Platform.Contracts.Catalog.Pricing;

namespace Platform.Backoffice.Models;

public sealed class PriceListDetailsPageViewModel
{
    public PriceListUpdateViewModel PriceList { get; init; } = new();
    public IReadOnlyList<PriceListMarketAssignmentDto> Markets { get; init; } = [];
    public IReadOnlyList<PriceListEntryDto> Entries { get; init; } = [];
    public PriceListMarketAssignmentCreateViewModel MarketAssignmentForm { get; init; } = new();
    public PriceListEntryCreateViewModel EntryForm { get; init; } = new();
}
