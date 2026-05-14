using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Security;
using Platform.Application.Security.AdminUsers;
using Platform.Application.Security.AdminUsers.Commands;
using Platform.Application.Security.AdminUsers.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Security;
using Platform.Domain.Security;

namespace Platform.Infrastructure.Security.AdminUsers;

public sealed class AdminUserAdminApplicationService : IAdminUserAdminApplicationService
{
    private static readonly string[] AllowedStatuses = ["Active", "Inactive"];

    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminUserAdminApplicationService(
        IAdminUserRepository adminUserRepository,
        IUnitOfWork unitOfWork)
    {
        _adminUserRepository = adminUserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<AdminUserSummaryDto>> ListAsync(ListAdminUsersQuery query, CancellationToken cancellationToken)
    {
        var result = await _adminUserRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<AdminUserSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<AdminUserDetailsDto?> GetByIdAsync(GetAdminUserByIdQuery query, CancellationToken cancellationToken)
    {
        var adminUser = await _adminUserRepository.GetByIdAsync(query.AdminUserId, cancellationToken);
        return adminUser is null ? null : MapDetails(adminUser);
    }

    public async Task<AdminUserDetailsDto> CreateAsync(CreateAdminUserCommand command, CancellationToken cancellationToken)
    {
        await EnsureUsernameUniqueAsync(command.Username, null, cancellationToken);
        ValidateStatus(command.Status);
        ValidateRoles(command.Roles);

        var now = DateTime.UtcNow;
        var adminUser = new AdminUser(
            Guid.NewGuid(),
            command.Username,
            BootstrapCredentialVerifier.HashPassword(command.Password),
            command.DisplayName,
            command.Status,
            command.Roles,
            now,
            now);

        await _adminUserRepository.AddAsync(adminUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(adminUser);
    }

    public async Task<AdminUserDetailsDto?> UpdateAsync(UpdateAdminUserCommand command, CancellationToken cancellationToken)
    {
        var adminUser = await _adminUserRepository.GetByIdAsync(command.AdminUserId, cancellationToken);
        if (adminUser is null)
        {
            return null;
        }

        ValidateStatus(command.Status);
        ValidateRoles(command.Roles);

        adminUser.Update(command.DisplayName, command.Status, command.Roles, command.RowVersion);

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            adminUser.SetPasswordHash(BootstrapCredentialVerifier.HashPassword(command.Password), adminUser.RowVersion);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(adminUser);
    }

    private async Task EnsureUsernameUniqueAsync(string username, Guid? currentAdminUserId, CancellationToken cancellationToken)
    {
        var existing = await _adminUserRepository.GetByUsernameAsync(username.Trim().ToUpperInvariant(), cancellationToken);
        if (existing is not null && existing.Id != currentAdminUserId)
        {
            throw new ConflictException("Admin username already exists.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (!AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(CreateAdminUserCommand.Status), "Unknown admin user status.");
        }
    }

    private static void ValidateRoles(IReadOnlyList<string> roles)
    {
        if (roles.Count == 0)
        {
            throw new RequestValidationException(nameof(CreateAdminUserCommand.Roles), "At least one role is required.");
        }

        var allowedRoles = new[]
        {
            AdminRoles.PlatformAdmin,
            AdminRoles.CatalogManager,
            AdminRoles.CatalogViewer,
            AdminRoles.PricingManager,
            AdminRoles.CustomerService,
            AdminRoles.InventoryManager,
            AdminRoles.IntegrationClient
        };

        var invalidRole = roles.FirstOrDefault(role => !allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        if (invalidRole is not null)
        {
            throw new RequestValidationException(nameof(CreateAdminUserCommand.Roles), $"Unknown role '{invalidRole}'.");
        }
    }

    private static AdminUserSummaryDto MapSummary(AdminUser adminUser)
    {
        return new AdminUserSummaryDto(
            adminUser.Id,
            adminUser.Username,
            adminUser.DisplayName,
            adminUser.Status,
            adminUser.Roles.Select(x => x.Role).ToList(),
            adminUser.UpdatedAtUtc,
            adminUser.RowVersion);
    }

    private static AdminUserDetailsDto MapDetails(AdminUser adminUser)
    {
        return new AdminUserDetailsDto(
            adminUser.Id,
            adminUser.Username,
            adminUser.DisplayName,
            adminUser.Status,
            adminUser.Roles.Select(x => x.Role).ToList(),
            adminUser.CreatedAtUtc,
            adminUser.UpdatedAtUtc,
            adminUser.RowVersion);
    }
}
