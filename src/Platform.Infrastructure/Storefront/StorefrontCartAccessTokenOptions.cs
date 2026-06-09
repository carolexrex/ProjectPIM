namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontCartAccessTokenOptions
{
    public const string SectionName = "StorefrontSecurity:CartAccessToken";

    public string? SigningKey { get; init; }
}
