namespace Platform.Application.Abstractions.Errors;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string key, string message)
        : this(new Dictionary<string, string[]>
        {
            [key] = [message]
        })
    {
    }

    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("The request is invalid.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
