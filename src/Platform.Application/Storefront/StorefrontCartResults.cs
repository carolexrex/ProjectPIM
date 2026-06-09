using Platform.Contracts.Storefront;

namespace Platform.Application.Storefront;

public sealed record StorefrontCartResult(
    StorefrontCartDto? Cart,
    StorefrontContextResolutionStatus Status,
    string? ResourceName,
    string? ResourceKey,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static StorefrontCartResult Success(StorefrontCartDto cart)
    {
        return new StorefrontCartResult(cart, StorefrontContextResolutionStatus.Success, null, null, EmptyErrors);
    }

    public static StorefrontCartResult NotFound(string resourceName, string resourceKey)
    {
        return new StorefrontCartResult(null, StorefrontContextResolutionStatus.NotFound, resourceName, resourceKey, EmptyErrors);
    }

    public static StorefrontCartResult FromContextFailure(StorefrontContextResolutionResult contextResult)
    {
        return new StorefrontCartResult(
            null,
            contextResult.Status,
            contextResult.ResourceName,
            contextResult.ResourceKey,
            contextResult.Errors);
    }

    public static StorefrontCartResult Invalid(IReadOnlyDictionary<string, string[]> errors)
    {
        return new StorefrontCartResult(null, StorefrontContextResolutionStatus.ValidationFailed, null, null, errors);
    }

    public static StorefrontCartResult Invalid(string key, string message)
    {
        return Invalid(new Dictionary<string, string[]>
        {
            [key] = [message]
        });
    }

    public static StorefrontCartResult Unauthorized()
    {
        return new StorefrontCartResult(null, StorefrontContextResolutionStatus.Unauthorized, null, null, EmptyErrors);
    }

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();
}

public sealed record StorefrontCheckoutResult(
    StorefrontOrderDto? Order,
    StorefrontContextResolutionStatus Status,
    string? ResourceName,
    string? ResourceKey,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static StorefrontCheckoutResult Success(StorefrontOrderDto order)
    {
        return new StorefrontCheckoutResult(order, StorefrontContextResolutionStatus.Success, null, null, EmptyErrors);
    }

    public static StorefrontCheckoutResult NotFound(string resourceName, string resourceKey)
    {
        return new StorefrontCheckoutResult(null, StorefrontContextResolutionStatus.NotFound, resourceName, resourceKey, EmptyErrors);
    }

    public static StorefrontCheckoutResult Invalid(IReadOnlyDictionary<string, string[]> errors)
    {
        return new StorefrontCheckoutResult(null, StorefrontContextResolutionStatus.ValidationFailed, null, null, errors);
    }

    public static StorefrontCheckoutResult Invalid(string key, string message)
    {
        return Invalid(new Dictionary<string, string[]>
        {
            [key] = [message]
        });
    }

    public static StorefrontCheckoutResult Unauthorized()
    {
        return new StorefrontCheckoutResult(null, StorefrontContextResolutionStatus.Unauthorized, null, null, EmptyErrors);
    }

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();
}
