namespace Platform.Api.Security;

public sealed record AuthenticatedAdminUser(
    string SubjectId,
    string PrincipalType,
    string Username,
    string DisplayName,
    IReadOnlyList<string> Roles);
