namespace Platform.Domain.Security;

public sealed class AdminUserRoleAssignment
{
    private AdminUserRoleAssignment()
    {
        AdminUserId = Guid.Empty;
        Role = string.Empty;
    }

    internal AdminUserRoleAssignment(Guid adminUserId, string role)
    {
        AdminUserId = adminUserId;
        Role = string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim();
    }

    public Guid AdminUserId { get; private set; }
    public string Role { get; private set; }
}
