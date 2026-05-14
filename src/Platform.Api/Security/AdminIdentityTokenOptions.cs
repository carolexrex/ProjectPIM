namespace Platform.Api.Security;

public sealed class AdminIdentityTokenOptions
{
    public const string SectionName = "AdminIdentityToken";

    public int AccessTokenLifetimeMinutes { get; init; } = 480;
}
