using Platform.Application.Security.AdminUsers.Queries;
using Platform.Domain.Security;

namespace Platform.Application.Security.AdminUsers;

public interface IAdminUserRepository
{
    Task<AdminUserListResult> ListAsync(ListAdminUsersQuery query, CancellationToken cancellationToken);
    Task<AdminUser?> GetByIdAsync(Guid adminUserId, CancellationToken cancellationToken);
    Task<AdminUser?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);
    Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken);
}
