namespace Platform.Application.Catalog.Pricing.Commands;

public sealed record UpsertPriceListEntryCommand(
    Guid PriceListId,
    Guid? EntryId,
    string TargetType,
    Guid TargetId,
    int MinQuantity,
    decimal Amount,
    decimal? CompareAtAmount,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string RowVersion);
