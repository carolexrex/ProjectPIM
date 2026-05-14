namespace Platform.Application.Catalog.Pricing.Commands;

public sealed record UpdatePriceListCommand(
    Guid PriceListId,
    string Code,
    string Name,
    string CurrencyCode,
    bool VatIncluded,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string RowVersion);
