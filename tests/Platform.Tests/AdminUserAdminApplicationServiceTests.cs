using Platform.Application.Security.AdminUsers.Commands;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Security.AdminUsers;

namespace Platform.Tests;

public sealed class AdminUserAdminApplicationServiceTests
{
    [Fact]
    public async Task CreateAdminUser_HashesPasswordAndPersistsRoles()
    {
        var store = new InMemoryCatalogStore();
        var service = new AdminUserAdminApplicationService(
            new InMemoryAdminUserRepository(store),
            new InMemoryUnitOfWork());

        var created = await service.CreateAsync(
            new CreateAdminUserCommand(
                "ops-admin",
                "OpsPassword123!",
                "Operations Admin",
                "Active",
                ["PlatformAdmin", "CatalogManager"]),
            CancellationToken.None);

        Assert.Equal("ops-admin", created.Username);
        Assert.Contains("PlatformAdmin", created.Roles);

        var stored = store.AdminUsers.Values.Single(x => x.Username == "ops-admin");
        Assert.NotEqual("OpsPassword123!", stored.PasswordHash);
        Assert.True(Platform.Application.Security.BootstrapCredentialVerifier.VerifyHashedPassword("OpsPassword123!", stored.PasswordHash));
    }

    [Fact]
    public async Task UpdateAdminUser_RotatesPasswordAndUpdatesRoleSet()
    {
        var store = new InMemoryCatalogStore();
        var service = new AdminUserAdminApplicationService(
            new InMemoryAdminUserRepository(store),
            new InMemoryUnitOfWork());

        var created = await service.CreateAsync(
            new CreateAdminUserCommand(
                "support-admin",
                "StartPassword123!",
                "Support Admin",
                "Active",
                ["CatalogViewer"]),
            CancellationToken.None);

        var updated = await service.UpdateAsync(
            new UpdateAdminUserCommand(
                created.Id,
                "Support Lead",
                "Inactive",
                ["CustomerService"],
                "ChangedPassword123!",
                created.RowVersion),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Support Lead", updated!.DisplayName);
        Assert.Equal("Inactive", updated.Status);
        Assert.Single(updated.Roles);
        Assert.Equal("CustomerService", updated.Roles[0]);

        var stored = store.AdminUsers[created.Id];
        Assert.True(Platform.Application.Security.BootstrapCredentialVerifier.VerifyHashedPassword("ChangedPassword123!", stored.PasswordHash));
    }

    [Fact]
    public async Task CreateAdminUser_AllowsPricingManagerRole()
    {
        var store = new InMemoryCatalogStore();
        var service = new AdminUserAdminApplicationService(
            new InMemoryAdminUserRepository(store),
            new InMemoryUnitOfWork());

        var created = await service.CreateAsync(
            new CreateAdminUserCommand(
                "pricing-admin",
                "PricingPassword123!",
                "Pricing Admin",
                "Active",
                ["PricingManager"]),
            CancellationToken.None);

        Assert.Single(created.Roles);
        Assert.Equal("PricingManager", created.Roles[0]);
    }
}
