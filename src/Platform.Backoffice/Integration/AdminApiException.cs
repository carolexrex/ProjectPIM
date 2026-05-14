namespace Platform.Backoffice.Integration;

public sealed class AdminApiException : Exception
{
    public AdminApiException(string message, int? statusCode, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public int? StatusCode { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
