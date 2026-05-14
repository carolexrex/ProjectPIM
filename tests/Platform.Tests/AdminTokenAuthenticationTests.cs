using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Platform.Api.Security;

namespace Platform.Tests;

public sealed class AdminTokenAuthenticationTests
{
    [Fact]
    public async Task ConfiguredUserAuthenticationService_AuthenticatesHashedUser()
    {
        var passwordHash = Platform.Application.Security.BootstrapCredentialVerifier.HashPassword("secret-123");
        var options = Options.Create(new AdminSecurityOptions
        {
            Users =
            [
                new ConfiguredAdminUser
                {
                    Username = "admin",
                    DisplayName = "Platform Admin",
                    PasswordHash = passwordHash,
                    Roles = ["PlatformAdmin", "CatalogManager"]
                }
            ]
        });

        var service = new AdminConfiguredUserAuthenticationService(options, new Platform.Infrastructure.Security.AdminUsers.InMemoryAdminUserRepository(new Platform.Infrastructure.Catalog.InMemoryCatalogStore()));

        var user = await service.AuthenticateAsync("admin", "secret-123", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("AdminUser", user!.PrincipalType);
        Assert.Equal("admin", user!.Username);
        Assert.Equal("Platform Admin", user.DisplayName);
        Assert.Contains("PlatformAdmin", user.Roles);
    }

    [Fact]
    public void AccessTokenService_RoundTripsPrincipalUntilExpiry()
    {
        var dataProtectionProvider = new PassthroughDataProtectionProvider();
        var tokenOptions = Options.Create(new AdminIdentityTokenOptions
        {
            AccessTokenLifetimeMinutes = 30
        });
        var service = new AdminAccessTokenService(dataProtectionProvider, tokenOptions);
        var user = new AuthenticatedAdminUser(
            "catalog",
            "AdminUser",
            "catalog",
            "Catalog Manager",
            ["CatalogManager", "CatalogViewer"]);

        var payload = service.CreateToken(user, out var accessToken);
        var principal = service.Validate(accessToken);

        Assert.NotNull(principal);
        Assert.Equal("catalog", principal!.Identity?.Name);
        Assert.Equal("AdminUser", principal.FindFirst("principal_type")?.Value);
        Assert.Equal(payload.ExpiresAtUtc.ToString("O"), principal.FindFirst("access_token_expires_at")?.Value);
        Assert.Contains(principal.Claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value == "CatalogManager");
    }

    [Fact]
    public void AccessTokenService_RejectsExpiredTokens()
    {
        var dataProtectionProvider = new PassthroughDataProtectionProvider();
        var tokenOptions = Options.Create(new AdminIdentityTokenOptions
        {
            AccessTokenLifetimeMinutes = -1
        });
        var service = new AdminAccessTokenService(dataProtectionProvider, tokenOptions);
        var user = new AuthenticatedAdminUser("viewer", "AdminUser", "viewer", "Catalog Viewer", ["CatalogViewer"]);

        service.CreateToken(user, out var accessToken);

        Assert.Null(service.Validate(accessToken));
    }

    private sealed class PassthroughDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return new PassthroughDataProtector();
        }
    }

    private sealed class PassthroughDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return this;
        }

        public byte[] Protect(byte[] plaintext)
        {
            return plaintext;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return protectedData;
        }
    }
}
