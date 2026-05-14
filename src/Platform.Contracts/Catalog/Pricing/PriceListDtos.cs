namespace Platform.Contracts.Catalog.Pricing;

public sealed record PriceListMarketAssignmentDto(
    Guid MarketId,
    string MarketCode,
    string MarketName,
    int Priority,
    bool IsBasePriceList);

public sealed record PriceListEntryDto(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string TargetLabel,
    int MinQuantity,
    decimal Amount,
    decimal? CompareAtAmount,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc);

public sealed record PriceListSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string CurrencyCode,
    bool VatIncluded,
    string Status,
    int MarketCount,
    int EntryCount,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record PriceListDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string CurrencyCode,
    bool VatIncluded,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string Status,
    IReadOnlyList<PriceListMarketAssignmentDto> Markets,
    IReadOnlyList<PriceListEntryDto> Entries,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
