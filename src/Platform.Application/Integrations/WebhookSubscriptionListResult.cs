using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public sealed record WebhookSubscriptionListResult(
    IReadOnlyList<WebhookSubscription> Items,
    int Total);
