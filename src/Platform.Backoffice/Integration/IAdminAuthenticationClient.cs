using Platform.Contracts.Security;

namespace Platform.Backoffice.Integration;

public interface IAdminAuthenticationClient
{
    Task<AdminLoginResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken);
}
