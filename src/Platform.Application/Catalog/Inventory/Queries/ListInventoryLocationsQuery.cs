namespace Platform.Application.Catalog.Inventory.Queries;

public sealed record ListInventoryLocationsQuery(
    string? Search,
    string? Status,
    Guid? MarketId,
    int Page = 1,
    int PageSize = 50,
    string? Sort = null);
