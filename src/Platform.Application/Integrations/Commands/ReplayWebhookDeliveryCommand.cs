namespace Platform.Application.Integrations.Commands;

public sealed record ReplayWebhookDeliveryCommand(
    Guid WebhookDeliveryId,
    string RowVersion);
