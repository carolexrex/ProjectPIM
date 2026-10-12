namespace Platform.Application.Catalog.Tax.Queries;

public sealed record ListVatRulesQuery(
    Guid? MarketId,
    string? TaxCategoryCode,
    int Page = 1,
    int PageSize = 50);
