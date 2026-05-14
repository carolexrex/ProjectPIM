namespace Platform.Application.Integrations.Commands;

public sealed record CreateWebhookSubscriptionCommand(
    string Name,
    string EndpointUrl,
    string Secret,
    IReadOnlyList<string> EventTypes,
    bool IsActive);
