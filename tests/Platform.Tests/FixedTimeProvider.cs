namespace Platform.Tests;

/// <summary>
/// Deterministic clock for projection/refresh tests that assert on validity boundaries.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTime utcNow)
    {
        _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public void Set(DateTime utcNow)
    {
        _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
    }
}
