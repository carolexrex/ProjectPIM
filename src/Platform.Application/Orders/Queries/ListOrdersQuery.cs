namespace Platform.Application.Orders.Queries;

public sealed record ListOrdersQuery(
    string? Status,
    string? PaymentStatus,
    string? FulfillmentStatus,
    Guid? CustomerId,
    Guid? CompanyId,
    Guid? MarketId,
    DateTime? PlacedFromUtc,
    DateTime? PlacedToUtc,
    string? Search,
    int Page,
    int PageSize,
    string? Sort);
