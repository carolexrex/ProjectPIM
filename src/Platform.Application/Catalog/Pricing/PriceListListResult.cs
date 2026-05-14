using Platform.Domain.Catalog.Pricing;

namespace Platform.Application.Catalog.Pricing;

public sealed record PriceListListResult(
    IReadOnlyList<PriceList> Items,
    int Total);
