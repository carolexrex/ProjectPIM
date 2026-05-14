using Platform.Contracts.Catalog.Markets;

namespace Platform.Backoffice.Models;

public sealed class MarketDetailsPageViewModel
{
    public MarketUpdateViewModel Market { get; init; } = new();
    public IReadOnlyList<MarketCurrencyDto> Currencies { get; init; } = [];
    public IReadOnlyList<MarketCultureDto> Cultures { get; init; } = [];
    public IReadOnlyList<MarketProductAssignmentDto> ProductAssignments { get; init; } = [];
    public MarketCurrenciesFormViewModel CurrenciesForm { get; init; } = new();
    public MarketCulturesFormViewModel CulturesForm { get; init; } = new();
    public MarketProductAssignmentCreateViewModel ProductAssignmentForm { get; init; } = new();
}
