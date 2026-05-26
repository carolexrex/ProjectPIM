namespace Platform.Application.Integrations.Commands;

public sealed record ResetStorefrontProjectionRefreshMessageCommand(
    Guid OutboxMessageId,
    string RowVersion);
