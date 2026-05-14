using System.Text.Json;
using Platform.Domain.Common;

namespace Platform.Domain.Integrations;

public sealed class WebhookSubscription
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private WebhookSubscription()
    {
        Id = Guid.Empty;
        Name = string.Empty;
        EndpointUrl = string.Empty;
        Secret = string.Empty;
        EventTypesJson = "[]";
        RowVersion = string.Empty;
    }

    public WebhookSubscription(
        Guid id,
        string name,
        string endpointUrl,
        string secret,
        IReadOnlyCollection<string> eventTypes,
        bool isActive,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = NormalizeRequired(name);
        EndpointUrl = NormalizeRequired(endpointUrl);
        Secret = NormalizeRequired(secret);
        EventTypesJson = SerializeEventTypes(eventTypes);
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string EndpointUrl { get; private set; }
    public string Secret { get; private set; }
    public string EventTypesJson { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }

    public IReadOnlyList<string> GetEventTypes()
    {
        return JsonSerializer.Deserialize<IReadOnlyList<string>>(EventTypesJson, JsonOptions) ?? [];
    }

    public bool SupportsEventType(string eventType)
    {
        return IsActive && GetEventTypes().Any(x => string.Equals(x, eventType, StringComparison.OrdinalIgnoreCase));
    }

    public void Update(string name, string endpointUrl, string secret, IReadOnlyCollection<string> eventTypes, bool isActive, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        Name = NormalizeRequired(name);
        EndpointUrl = NormalizeRequired(endpointUrl);
        Secret = NormalizeRequired(secret);
        EventTypesJson = SerializeEventTypes(eventTypes);
        IsActive = isActive;
        Touch();
    }

    public void SetActive(bool isActive, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        IsActive = isActive;
        Touch();
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The webhook subscription has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string SerializeEventTypes(IReadOnlyCollection<string> eventTypes)
    {
        var normalized = eventTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return JsonSerializer.Serialize(normalized, JsonOptions);
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
