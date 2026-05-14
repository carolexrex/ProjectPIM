namespace Platform.Contracts.Security;

public sealed record AdminLoginRequest(
    string Username,
    string Password);

public sealed record AdminLoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string PrincipalType,
    string Username,
    string DisplayName,
    IReadOnlyList<string> Roles);
