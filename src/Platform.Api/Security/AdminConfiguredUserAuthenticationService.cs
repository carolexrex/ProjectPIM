using Microsoft.Extensions.Options;
using Platform.Application.Security.AdminUsers;
using Platform.Application.Security;

namespace Platform.Api.Security;

public sealed class AdminConfiguredUserAuthenticationService
{
    private readonly IOptions<AdminSecurityOptions> _securityOptions;
    private readonly IAdminUserRepository _adminUserRepository;

    public AdminConfiguredUserAuthenticationService(
        IOptions<AdminSecurityOptions> securityOptions,
        IAdminUserRepository adminUserRepository)
    {
        _securityOptions = securityOptions;
        _adminUserRepository = adminUserRepository;
    }

    public async Task<AuthenticatedAdminUser?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();
        var storedUser = await _adminUserRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);
        if (storedUser is not null
            && storedUser.IsActive()
            && BootstrapCredentialVerifier.VerifyHashedPassword(password, storedUser.PasswordHash))
        {
            return new AuthenticatedAdminUser(
                storedUser.Id.ToString(),
                "AdminUser",
                storedUser.Username,
                storedUser.DisplayName,
                storedUser.Roles.Select(x => x.Role).ToList());
        }

        var user = _securityOptions.Value.Users.FirstOrDefault(x =>
            string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));

        if (user is null || !BootstrapCredentialVerifier.Matches(password, user.Password, user.PasswordHash))
        {
            return null;
        }

        var displayName = string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.Username
            : user.DisplayName;

        return new AuthenticatedAdminUser(user.Username, user.PrincipalType, user.Username, displayName, user.Roles);
    }
}
