namespace Platform.Domain.Auditing;

public sealed class AuditLog
{
    private AuditLog()
    {
        Id = Guid.Empty;
        EntityType = string.Empty;
        EntityId = string.Empty;
        Action = string.Empty;
        ActorIdentifier = string.Empty;
        ActorDisplayName = string.Empty;
        ActorType = string.Empty;
        ChangedFieldsJson = "[]";
    }

    public AuditLog(
        Guid id,
        string entityType,
        string entityId,
        string action,
        string actorIdentifier,
        string actorDisplayName,
        string actorType,
        string changedFieldsJson,
        DateTime occurredAtUtc)
    {
        Id = id;
        EntityType = NormalizeRequired(entityType);
        EntityId = NormalizeRequired(entityId);
        Action = NormalizeRequired(action);
        ActorIdentifier = NormalizeRequired(actorIdentifier);
        ActorDisplayName = NormalizeRequired(actorDisplayName);
        ActorType = NormalizeRequired(actorType);
        ChangedFieldsJson = string.IsNullOrWhiteSpace(changedFieldsJson) ? "[]" : changedFieldsJson.Trim();
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public string EntityType { get; private set; }
    public string EntityId { get; private set; }
    public string Action { get; private set; }
    public string ActorIdentifier { get; private set; }
    public string ActorDisplayName { get; private set; }
    public string ActorType { get; private set; }
    public string ChangedFieldsJson { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
