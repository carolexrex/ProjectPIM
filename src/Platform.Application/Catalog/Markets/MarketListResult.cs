using Platform.Domain.Catalog.Markets;

namespace Platform.Application.Catalog.Markets;

public sealed record MarketListResult(
    IReadOnlyList<Market> Items,
    int Total);
