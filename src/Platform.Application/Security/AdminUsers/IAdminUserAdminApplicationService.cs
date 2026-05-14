using Platform.Application.Security.AdminUsers.Commands;
using Platform.Application.Security.AdminUsers.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Security;

namespace Platform.Application.Security.AdminUsers;

public interface IAdminUserAdminApplicationService
{
    Task<PagedResponse<AdminUserSummaryDto>> ListAsync(ListAdminUsersQuery query, CancellationToken cancellationToken);
    Task<AdminUserDetailsDto?> GetByIdAsync(GetAdminUserByIdQuery query, CancellationToken cancellationToken);
    Task<AdminUserDetailsDto> CreateAsync(CreateAdminUserCommand command, CancellationToken cancellationToken);
    Task<AdminUserDetailsDto?> UpdateAsync(UpdateAdminUserCommand command, CancellationToken cancellationToken);
}
