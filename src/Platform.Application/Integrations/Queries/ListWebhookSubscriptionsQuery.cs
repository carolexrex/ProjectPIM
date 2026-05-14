namespace Platform.Application.Integrations.Queries;

public sealed record ListWebhookSubscriptionsQuery(
    string? Search,
    bool? IsActive,
    string? EventType,
    int Page,
    int PageSize,
    string? Sort);
