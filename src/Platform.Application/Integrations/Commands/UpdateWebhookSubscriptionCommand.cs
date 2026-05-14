namespace Platform.Application.Integrations.Commands;

public sealed record UpdateWebhookSubscriptionCommand(
    Guid WebhookSubscriptionId,
    string Name,
    string EndpointUrl,
    string Secret,
    IReadOnlyList<string> EventTypes,
    bool IsActive,
    string RowVersion);
