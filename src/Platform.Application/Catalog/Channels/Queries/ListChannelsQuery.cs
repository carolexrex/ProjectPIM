namespace Platform.Application.Catalog.Channels.Queries;

public sealed record ListChannelsQuery(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 50,
    string? Sort = null);
