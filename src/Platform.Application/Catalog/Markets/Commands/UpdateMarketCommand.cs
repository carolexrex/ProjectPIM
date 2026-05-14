namespace Platform.Application.Catalog.Markets.Commands;

public sealed record UpdateMarketCommand(
    Guid MarketId,
    string Code,
    string Name,
    string DefaultCurrency,
    string DefaultCulture,
    string VatMode,
    string RowVersion);
