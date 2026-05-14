namespace Platform.Application.Security.AdminUsers.Commands;

public sealed record CreateAdminUserCommand(
    string Username,
    string Password,
    string DisplayName,
    string Status,
    IReadOnlyList<string> Roles);
