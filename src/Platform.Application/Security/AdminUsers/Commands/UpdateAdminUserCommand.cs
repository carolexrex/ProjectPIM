namespace Platform.Application.Security.AdminUsers.Commands;

public sealed record UpdateAdminUserCommand(
    Guid AdminUserId,
    string DisplayName,
    string Status,
    IReadOnlyList<string> Roles,
    string? Password,
    string RowVersion);
