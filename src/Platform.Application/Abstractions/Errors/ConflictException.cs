namespace Platform.Application.Abstractions.Errors;

public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
