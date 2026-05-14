namespace Platform.Api.Security;

public sealed class AdminSecurityOptions
{
    public const string SectionName = "AdminSecurity";

    public IReadOnlyList<ConfiguredAdminUser> Users { get; init; } = [];
}
