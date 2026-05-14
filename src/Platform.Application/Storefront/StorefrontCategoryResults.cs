using Platform.Contracts.Storefront;

namespace Platform.Application.Storefront;

public sealed record StorefrontCategoryListResult(
    IReadOnlyList<StorefrontCategoryNodeDto>? Categories,
    StorefrontContextDto? Context,
    StorefrontContextResolutionStatus Status,
    string? ResourceName,
    string? ResourceKey,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static StorefrontCategoryListResult Success(
        IReadOnlyList<StorefrontCategoryNodeDto> categories,
        StorefrontContextDto context)
    {
        return new StorefrontCategoryListResult(categories, context, StorefrontContextResolutionStatus.Success, null, null, EmptyErrors);
    }

    public static StorefrontCategoryListResult FromContextFailure(StorefrontContextResolutionResult contextResult)
    {
        return new StorefrontCategoryListResult(
            null,
            null,
            contextResult.Status,
            contextResult.ResourceName,
            contextResult.ResourceKey,
            contextResult.Errors);
    }

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();
}

public sealed record StorefrontCategoryDetailsResult(
    StorefrontCategoryDetailsDto? Category,
    StorefrontContextDto? Context,
    StorefrontContextResolutionStatus Status,
    string? ResourceName,
    string? ResourceKey,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static StorefrontCategoryDetailsResult Success(
        StorefrontCategoryDetailsDto category,
        StorefrontContextDto context)
    {
        return new StorefrontCategoryDetailsResult(category, context, StorefrontContextResolutionStatus.Success, null, null, EmptyErrors);
    }

    public static StorefrontCategoryDetailsResult NotFound(
        StorefrontContextDto context,
        string resourceName,
        string resourceKey)
    {
        return new StorefrontCategoryDetailsResult(null, context, StorefrontContextResolutionStatus.NotFound, resourceName, resourceKey, EmptyErrors);
    }

    public static StorefrontCategoryDetailsResult FromContextFailure(StorefrontContextResolutionResult contextResult)
    {
        return new StorefrontCategoryDetailsResult(
            null,
            null,
            contextResult.Status,
            contextResult.ResourceName,
            contextResult.ResourceKey,
            contextResult.Errors);
    }

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors = new Dictionary<string, string[]>();
}
