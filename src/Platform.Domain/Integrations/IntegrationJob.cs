using Platform.Domain.Common;

namespace Platform.Domain.Integrations;

public sealed class IntegrationJob
{
    private IntegrationJob()
    {
        Id = Guid.Empty;
        Type = string.Empty;
        Direction = string.Empty;
        Status = string.Empty;
        RequestedBy = string.Empty;
        RowVersion = string.Empty;
    }

    public IntegrationJob(
        Guid id,
        string type,
        string direction,
        string requestedBy,
        string? payloadJson,
        DateTime createdAtUtc)
    {
        Id = id;
        Type = NormalizeRequired(type);
        Direction = NormalizeRequired(direction);
        Status = IntegrationJobStatuses.Pending;
        RequestedBy = NormalizeRequired(requestedBy);
        PayloadJson = NormalizeOptional(payloadJson);
        AttemptCount = 0;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Direction { get; private set; }
    public string Status { get; private set; }
    public string RequestedBy { get; private set; }
    public string? PayloadJson { get; private set; }
    public string? ResultPayloadJson { get; private set; }
    public string? ResultSummary { get; private set; }
    public string? LastError { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }

    public bool CanStartAt(DateTime nowUtc)
    {
        return Status switch
        {
            IntegrationJobStatuses.Pending => true,
            IntegrationJobStatuses.Failed => !NextAttemptAtUtc.HasValue || NextAttemptAtUtc.Value <= nowUtc,
            _ => false
        };
    }

    public void Start(string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, IntegrationJobStatuses.Pending, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Status, IntegrationJobStatuses.Failed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only pending or failed jobs can be started.");
        }

        Status = IntegrationJobStatuses.Running;
        StartedAtUtc = DateTime.UtcNow;
        CompletedAtUtc = null;
        NextAttemptAtUtc = null;
        LastError = null;
        AttemptCount++;
        Touch();
    }

    public void Complete(string? resultSummary, string? resultPayloadJson, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, IntegrationJobStatuses.Running, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only running jobs can be completed.");
        }

        Status = IntegrationJobStatuses.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ResultSummary = NormalizeOptional(resultSummary);
        ResultPayloadJson = NormalizeOptional(resultPayloadJson);
        LastError = null;
        NextAttemptAtUtc = null;
        Touch();
    }

    public void Fail(string error, DateTime? nextAttemptAtUtc, string rowVersion)
    {
        EnsureRowVersion(rowVersion);

        if (!string.Equals(Status, IntegrationJobStatuses.Running, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only running jobs can be failed.");
        }

        Status = IntegrationJobStatuses.Failed;
        CompletedAtUtc = null;
        LastError = NormalizeRequired(error);
        NextAttemptAtUtc = nextAttemptAtUtc;
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The integration job has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
