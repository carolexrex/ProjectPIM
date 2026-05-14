using Platform.Application.Abstractions.Errors;
using Platform.Application.Companies.Commands;
using Platform.Application.Customers.Commands;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Companies;
using Platform.Infrastructure.Customers;
using Platform.Infrastructure.Persistence;

namespace Platform.Tests;

public sealed class CustomerCompanyAdminApplicationServiceTests
{
    [Fact]
    public async Task CreateCustomer_RejectsDuplicateRegisteredEmail()
    {
        var store = new InMemoryCatalogStore();
        var service = new CustomerAdminApplicationService(
            new InMemoryCustomerRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(
                new CreateCustomerCommand(
                    ExternalId: null,
                    UserId: null,
                    Email: "buyer@example.com",
                    FirstName: "Another",
                    LastName: "Buyer",
                    Phone: null,
                    PreferredCulture: null,
                    DefaultMarketId: null,
                    Status: "Active",
                    IsGuest: false),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateMembership_RejectsInvalidValidityWindow()
    {
        var store = new InMemoryCatalogStore();
        var service = new CompanyAdminApplicationService(
            new InMemoryCompanyRepository(store),
            new InMemoryCustomerRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateMembershipAsync(
                new CreateCompanyMembershipCommand(
                    Guid.Parse("77000000-0000-0000-0000-000000000001"),
                    Guid.Parse("76000000-0000-0000-0000-000000000001"),
                    "Buyer",
                    "Active",
                    false,
                    true,
                    false,
                    false,
                    DateTime.UtcNow.Date,
                    DateTime.UtcNow.Date.AddDays(-1)),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAndUpdateMembership_PersistsPermissionFlags()
    {
        var store = new InMemoryCatalogStore();
        var customerService = new CustomerAdminApplicationService(
            new InMemoryCustomerRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryUnitOfWork());
        var companyService = new CompanyAdminApplicationService(
            new InMemoryCompanyRepository(store),
            new InMemoryCustomerRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryUnitOfWork());

        var customer = await customerService.CreateAsync(
            new CreateCustomerCommand(
                ExternalId: null,
                UserId: null,
                Email: "permissions@example.com",
                FirstName: "Permissions",
                LastName: "Tester",
                Phone: null,
                PreferredCulture: null,
                DefaultMarketId: null,
                Status: "Active",
                IsGuest: false),
            CancellationToken.None);

        var membership = await companyService.CreateMembershipAsync(
            new CreateCompanyMembershipCommand(
                Guid.Parse("77000000-0000-0000-0000-000000000001"),
                customer.Id,
                "Approver",
                "Active",
                false,
                false,
                true,
                true,
                null,
                null),
            CancellationToken.None);

        Assert.NotNull(membership);
        Assert.False(membership.CanPlaceOrders);
        Assert.True(membership.CanApproveOrders);
        Assert.True(membership.CanManageUsers);

        var updated = await companyService.UpdateMembershipAsync(
            new UpdateCompanyMembershipCommand(
                membership!.Id,
                "Buyer",
                "Inactive",
                true,
                true,
                false,
                false,
                null,
                null,
                membership.RowVersion),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.True(updated!.IsDefaultCompany);
        Assert.True(updated.CanPlaceOrders);
        Assert.False(updated.CanApproveOrders);
        Assert.False(updated.CanManageUsers);
        Assert.Equal("Inactive", updated.Status);
    }
}
