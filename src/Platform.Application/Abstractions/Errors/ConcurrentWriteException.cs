namespace Platform.Application.Abstractions.Errors;

/// <summary>
/// Raised by a unit of work when the database rejects a save because another operation changed or
/// claimed the same rows first (stale concurrency token or unique-key race). Callers may retry the
/// whole operation against fresh state; if it surfaces, it maps to 409 Conflict.
/// </summary>
public sealed class ConcurrentWriteException : Exception
{
    public ConcurrentWriteException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
