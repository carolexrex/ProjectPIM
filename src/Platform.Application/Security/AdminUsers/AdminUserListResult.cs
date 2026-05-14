using Platform.Domain.Security;

namespace Platform.Application.Security.AdminUsers;

public sealed record AdminUserListResult(
    IReadOnlyList<AdminUser> Items,
    int Total);
