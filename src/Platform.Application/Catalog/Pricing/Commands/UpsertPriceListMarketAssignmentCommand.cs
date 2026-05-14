namespace Platform.Application.Catalog.Pricing.Commands;

public sealed record UpsertPriceListMarketAssignmentCommand(
    Guid PriceListId,
    Guid MarketId,
    int Priority,
    bool IsBasePriceList,
    string RowVersion);
