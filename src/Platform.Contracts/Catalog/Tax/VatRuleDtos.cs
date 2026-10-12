namespace Platform.Contracts.Catalog.Tax;

public sealed record VatRuleDto(
    Guid Id,
    Guid MarketId,
    string MarketCode,
    string TaxCategoryCode,
    decimal Rate,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
