namespace Platform.Application.Catalog.Pricing.Commands;

public sealed record CreatePriceListCommand(
    string Code,
    string Name,
    string CurrencyCode,
    bool VatIncluded,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc);
