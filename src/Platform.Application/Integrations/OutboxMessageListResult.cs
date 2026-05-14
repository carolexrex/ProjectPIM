using Platform.Domain.Integrations;

namespace Platform.Application.Integrations;

public sealed record OutboxMessageListResult(
    IReadOnlyList<OutboxMessage> Items,
    int Total);
