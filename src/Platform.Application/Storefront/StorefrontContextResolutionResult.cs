using Platform.Contracts.Storefront;

namespace Platform.Application.Storefront;

public sealed record StorefrontContextResolutionResult(
    StorefrontContextDto? Context,
    StorefrontContextResolutionStatus Status,
    string? ResourceName,
    string? ResourceKey,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static StorefrontContextResolutionResult Success(StorefrontContextDto context)
    {
        return new StorefrontContextResolutionResult(context, StorefrontContextResolutionStatus.Success, null, null, EmptyErrors);
    }

    public static StorefrontContextResolutionResult NotFound(string resourceName, string resourceKey)
    {
        return new StorefrontContextResolutionResult(null, StorefrontContextResolutionStatus.NotFound, resourceName, resourceKey, EmptyErrors);
    }

    public static StorefrontContextResolutionResult Invalid(string key, string message)
    {
        return new StorefrontContextResolutionResult(
            null,
            StorefrontContextResolutionStatus.ValidationFailed,
            null,
            null,
            new Dictionary<string, string[]>
            {
                [key] = [message]
            });
    }

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();
}

public enum StorefrontContextResolutionStatus
{
    Success = 0,
    NotFound = 1,
    ValidationFailed = 2
}
