namespace Platform.Application.Catalog.Pricing.Commands;

public sealed record RemovePriceListEntryCommand(
    Guid PriceListId,
    Guid EntryId,
    string RowVersion);
