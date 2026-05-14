namespace Platform.Api.Security;

public sealed record AdminAccessTokenPayload(
    string SubjectId,
    string PrincipalType,
    string Username,
    string DisplayName,
    IReadOnlyList<string> Roles,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc);
