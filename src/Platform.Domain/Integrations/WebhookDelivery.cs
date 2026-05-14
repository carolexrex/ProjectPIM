using Platform.Domain.Common;

namespace Platform.Domain.Integrations;

public sealed class WebhookDelivery
{
    private WebhookDelivery()
    {
        Id = Guid.Empty;
        EventType = string.Empty;
        Status = string.Empty;
        PayloadJson = string.Empty;
        RowVersion = string.Empty;
    }

    public WebhookDelivery(
        Guid id,
        Guid webhookSubscriptionId,
        Guid eventId,
        string eventType,
        string payloadJson,
        DateTime createdAtUtc)
    {
        Id = id;
        WebhookSubscriptionId = webhookSubscriptionId;
        EventId = eventId;
        EventType = NormalizeRequired(eventType);
        Status = WebhookDeliveryStatuses.Pending;
        PayloadJson = NormalizeRequired(payloadJson);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public Guid WebhookSubscriptionId { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; }
    public string Status { get; private set; }
    public string PayloadJson { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public int? ResponseCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }

    public bool CanAttemptAt(DateTime nowUtc)
    {
        return Status switch
        {
            WebhookDeliveryStatuses.Pending => true,
            WebhookDeliveryStatuses.Failed => !NextAttemptAtUtc.HasValue || NextAttemptAtUtc.Value <= nowUtc,
            _ => false
        };
    }

    public void Start(string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!CanAttemptAt(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Only pending or retryable failed deliveries can be started.");
        }

        Status = WebhookDeliveryStatuses.Processing;
        AttemptCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
        NextAttemptAtUtc = null;
        ResponseCode = null;
        ResponseBody = null;
        Touch();
    }

    public void MarkSucceeded(int? responseCode, string? responseBody, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, WebhookDeliveryStatuses.Processing, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only processing deliveries can be marked as succeeded.");
        }

        Status = WebhookDeliveryStatuses.Succeeded;
        ResponseCode = responseCode;
        ResponseBody = NormalizeOptional(responseBody);
        NextAttemptAtUtc = null;
        Touch();
    }

    public void MarkFailed(int? responseCode, string? responseBody, DateTime? nextAttemptAtUtc, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, WebhookDeliveryStatuses.Processing, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only processing deliveries can be marked as failed.");
        }

        Status = WebhookDeliveryStatuses.Failed;
        ResponseCode = responseCode;
        ResponseBody = NormalizeOptional(responseBody);
        NextAttemptAtUtc = nextAttemptAtUtc;
        Touch();
    }

    public void MarkAbandoned(int? responseCode, string? responseBody, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, WebhookDeliveryStatuses.Processing, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only processing deliveries can be abandoned.");
        }

        Status = WebhookDeliveryStatuses.Abandoned;
        ResponseCode = responseCode;
        ResponseBody = NormalizeOptional(responseBody);
        NextAttemptAtUtc = null;
        Touch();
    }

    public void Replay(DateTime nextAttemptAtUtc, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, WebhookDeliveryStatuses.Failed, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Status, WebhookDeliveryStatuses.Abandoned, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only failed or abandoned deliveries can be replayed.");
        }

        Status = WebhookDeliveryStatuses.Failed;
        NextAttemptAtUtc = nextAttemptAtUtc;
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The webhook delivery has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
