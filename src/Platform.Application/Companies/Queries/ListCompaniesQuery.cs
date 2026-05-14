namespace Platform.Application.Companies.Queries;

public sealed record ListCompaniesQuery(
    string? Search,
    string? Status,
    Guid? DefaultMarketId,
    int Page,
    int PageSize,
    string? Sort);
