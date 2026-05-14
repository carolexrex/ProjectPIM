namespace Platform.Application.Integrations.Queries;

public sealed record ListWebhookDeliveriesQuery(
    Guid? WebhookSubscriptionId,
    string? EventType,
    string? Status,
    int Page,
    int PageSize,
    string? Sort);
