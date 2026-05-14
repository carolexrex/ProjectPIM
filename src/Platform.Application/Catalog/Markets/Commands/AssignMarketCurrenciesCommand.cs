namespace Platform.Application.Catalog.Markets.Commands;

public sealed record AssignMarketCurrenciesCommand(
    Guid MarketId,
    string DefaultCurrency,
    IReadOnlyList<string> Currencies,
    string RowVersion);
