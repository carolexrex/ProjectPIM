using Platform.Contracts.Common;
using Platform.Contracts.Storefront;

namespace Platform.Application.Storefront;

public sealed record StorefrontProductListResult(
    StorefrontProductListResponseDto? Products,
    StorefrontContextDto? Context,
    StorefrontContextResolutionStatus Status,
    string? ResourceName,
    string? ResourceKey,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static StorefrontProductListResult Success(
        StorefrontProductListResponseDto products,
        StorefrontContextDto context)
    {
        return new StorefrontProductListResult(products, context, StorefrontContextResolutionStatus.Success, null, null, EmptyErrors);
    }

    public static StorefrontProductListResult NotFound(
        StorefrontContextDto context,
        string resourceName,
        string resourceKey)
    {
        return new StorefrontProductListResult(null, context, StorefrontContextResolutionStatus.NotFound, resourceName, resourceKey, EmptyErrors);
    }

    public static StorefrontProductListResult FromContextFailure(StorefrontContextResolutionResult contextResult)
    {
        return new StorefrontProductListResult(
            null,
            null,
            contextResult.Status,
            contextResult.ResourceName,
            contextResult.ResourceKey,
            contextResult.Errors);
    }

    public static StorefrontProductListResult Invalid(string key, string message)
    {
        return new StorefrontProductListResult(
            null,
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

public sealed record StorefrontProductDetailsResult(
    StorefrontProductDetailsDto? Product,
    StorefrontContextDto? Context,
    StorefrontContextResolutionStatus Status,
    string? ResourceName,
    string? ResourceKey,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static StorefrontProductDetailsResult Success(
        StorefrontProductDetailsDto product,
        StorefrontContextDto context)
    {
        return new StorefrontProductDetailsResult(product, context, StorefrontContextResolutionStatus.Success, null, null, EmptyErrors);
    }

    public static StorefrontProductDetailsResult NotFound(
        StorefrontContextDto context,
        string resourceName,
        string resourceKey)
    {
        return new StorefrontProductDetailsResult(null, context, StorefrontContextResolutionStatus.NotFound, resourceName, resourceKey, EmptyErrors);
    }

    public static StorefrontProductDetailsResult FromContextFailure(StorefrontContextResolutionResult contextResult)
    {
        return new StorefrontProductDetailsResult(
            null,
            null,
            contextResult.Status,
            contextResult.ResourceName,
            contextResult.ResourceKey,
            contextResult.Errors);
    }

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();
}
