using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public sealed record WebhookDeliveryListResult(
    IReadOnlyList<WebhookDelivery> Items,
    int Total);
