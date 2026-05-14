namespace Platform.Application.Cart.Queries;

public sealed record ListCartsQuery(
    string? Status,
    Guid? CustomerId,
    Guid? CompanyId,
    Guid? MarketId,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    int Page,
    int PageSize,
    string? Sort);
