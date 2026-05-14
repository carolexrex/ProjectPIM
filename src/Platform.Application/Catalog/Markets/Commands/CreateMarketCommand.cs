namespace Platform.Application.Catalog.Markets.Commands;

public sealed record CreateMarketCommand(
    string Code,
    string Name,
    string DefaultCurrency,
    string DefaultCulture,
    string VatMode);
