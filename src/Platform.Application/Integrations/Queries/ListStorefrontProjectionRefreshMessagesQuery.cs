namespace Platform.Application.Integrations.Queries;

public sealed record ListStorefrontProjectionRefreshMessagesQuery(
    string? Status,
    int Page,
    int PageSize,
    string? Sort);
