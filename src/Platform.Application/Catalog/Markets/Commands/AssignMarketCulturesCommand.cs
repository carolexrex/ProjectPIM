namespace Platform.Application.Catalog.Markets.Commands;

public sealed record AssignMarketCulturesCommand(
    Guid MarketId,
    string DefaultCulture,
    IReadOnlyList<string> Cultures,
    string RowVersion);
