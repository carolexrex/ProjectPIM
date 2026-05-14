namespace Platform.Application.Catalog.Pricing.Commands;

public sealed record RemovePriceListMarketAssignmentCommand(
    Guid PriceListId,
    Guid MarketId,
    string RowVersion);
