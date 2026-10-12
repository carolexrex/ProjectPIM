namespace Platform.Application.Catalog.Tax.Commands;

public sealed record CreateVatRuleCommand(
    Guid MarketId,
    string TaxCategoryCode,
    decimal Rate,
    string? Description);
