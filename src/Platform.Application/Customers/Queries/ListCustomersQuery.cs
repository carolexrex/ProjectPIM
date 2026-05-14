namespace Platform.Application.Customers.Queries;

public sealed record ListCustomersQuery(
    string? Search,
    string? Status,
    bool? IsGuest,
    Guid? DefaultMarketId,
    int Page,
    int PageSize,
    string? Sort);
