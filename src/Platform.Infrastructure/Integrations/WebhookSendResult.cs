namespace Platform.Infrastructure.Integrations;

public sealed record WebhookSendResult(
    bool IsSuccess,
    int? ResponseCode,
    string? ResponseBody);
