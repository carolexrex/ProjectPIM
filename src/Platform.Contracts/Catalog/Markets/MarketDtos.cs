namespace Platform.Contracts.Catalog.Markets;

public sealed record MarketCurrencyDto(
    string CurrencyCode,
    bool IsDefault);

public sealed record MarketCultureDto(
    string CultureCode,
    bool IsDefault);

public sealed record MarketProductAssignmentDto(
    Guid ProductId,
    string ProductNumber,
    string? ProductName,
    string Status);

public sealed record MarketSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string DefaultCurrency,
    string DefaultCulture,
    string Status,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record MarketDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string DefaultCurrency,
    string DefaultCulture,
    string VatMode,
    string Status,
    IReadOnlyList<MarketCurrencyDto> Currencies,
    IReadOnlyList<MarketCultureDto> Cultures,
    IReadOnlyList<MarketProductAssignmentDto> ProductAssignments,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record MarketLookupDto(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyList<string> CurrencyCodes);
