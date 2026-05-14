namespace Platform.Api.Security;

public sealed class ConfiguredAdminUser
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string PrincipalType { get; init; } = "AdminUser";
    public IReadOnlyList<string> Roles { get; init; } = [];
}
