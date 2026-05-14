using Platform.Application.Abstractions.Errors;
using Platform.Application.Companies.Commands;
using Platform.Application.Customers.Commands;
using Platform.Application.Orders.Commands;
using Platform.Infrastructure.Cart;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Pricing;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Companies;
using Platform.Infrastructure.Customers;
using Platform.Infrastructure.Orders;
using Platform.Infrastructure.Persistence;

namespace Platform.Tests;

public sealed class CartOrderAdminApplicationServiceTests
{
    [Fact]
    public async Task CreateFromCart_IsIdempotentAndKeepsSnapshotData()
    {
        var store = new InMemoryCatalogStore();
        var cartRepository = new InMemoryCartRepository(store);
        var orderRepository = new InMemoryOrderRepository(store);
        var cartService = new CartAdminApplicationService(
            cartRepository,
            new InMemoryPriceListRepository(store),
            new InMemoryVariantRepository(store),
            new InMemoryProductRepository(store),
            new InMemoryUnitOfWork());
        var orderService = new OrderAdminApplicationService(
            orderRepository,
            cartRepository,
            new InMemoryCompanyRepository(store),
            new InMemoryCustomerRepository(store),
            new InMemoryMarketRepository(store),
            cartService,
            new InMemoryUnitOfWork());

        var cart = store.Carts[Guid.Parse("78000000-0000-0000-0000-000000000001")];
        var command = new CreateOrderCommand(
            CartId: cart.Id,
            CartRowVersion: cart.RowVersion,
            CustomerId: null,
            CompanyId: null,
            MarketId: null,
            CurrencyCode: null,
            CultureCode: null,
            Email: null,
            Lines: [],
            Addresses: []);

        var created = await orderService.CreateAsync(command, "tester", CancellationToken.None);
        var second = await orderService.CreateAsync(command, "tester", CancellationToken.None);

        Assert.Equal(created.Id, second.Id);
        Assert.Equal("Converted", store.Carts[cart.Id].Status);
        Assert.Single(created.Lines);
        Assert.Equal("SKU-EXAMPLE-1-BLACK", created.Lines[0].Sku);
        Assert.Equal("Example Drill", created.Lines[0].ProductName);
    }

    [Fact]
    public async Task ChangeStatus_RejectsIllegalTransition()
    {
        var store = new InMemoryCatalogStore();
        var orderService = new OrderAdminApplicationService(
            new InMemoryOrderRepository(store),
            new InMemoryCartRepository(store),
            new InMemoryCompanyRepository(store),
            new InMemoryCustomerRepository(store),
            new InMemoryMarketRepository(store),
            new CartAdminApplicationService(
                new InMemoryCartRepository(store),
                new InMemoryPriceListRepository(store),
                new InMemoryVariantRepository(store),
                new InMemoryProductRepository(store),
                new InMemoryUnitOfWork()),
            new InMemoryUnitOfWork());

        var created = await orderService.CreateAsync(
            new CreateOrderCommand(
                CartId: null,
                CartRowVersion: null,
                CustomerId: Guid.Parse("76000000-0000-0000-0000-000000000001"),
                CompanyId: null,
                MarketId: Guid.Parse("62000000-0000-0000-0000-000000000001"),
                CurrencyCode: "SEK",
                CultureCode: "sv-SE",
                Email: "buyer@example.com",
                Lines:
                [
                    new CreateOrderLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 1m, null)
                ],
                Addresses:
                [
                    new CreateOrderAddressItem("Billing", "Alicia", "Buyer", "Northwind Construction", "Sveavagen 10", null, "11157", "Stockholm", null, "SE", "buyer@example.com", "+46 70 100 10 10")
                ]),
            "tester",
            CancellationToken.None);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            orderService.ChangeStatusAsync(
                new ChangeOrderStatusCommand(created.Id, "Completed", null, created.RowVersion),
                "tester",
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateCompanyOrder_RejectsMembershipWithoutCanPlaceOrders()
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
        var orderService = new OrderAdminApplicationService(
            new InMemoryOrderRepository(store),
            new InMemoryCartRepository(store),
            new InMemoryCompanyRepository(store),
            new InMemoryCustomerRepository(store),
            new InMemoryMarketRepository(store),
            new CartAdminApplicationService(
                new InMemoryCartRepository(store),
                new InMemoryPriceListRepository(store),
                new InMemoryVariantRepository(store),
                new InMemoryProductRepository(store),
                new InMemoryUnitOfWork()),
            new InMemoryUnitOfWork());

        var customer = await customerService.CreateAsync(
            new CreateCustomerCommand(
                ExternalId: null,
                UserId: null,
                Email: "readonly-buyer@example.com",
                FirstName: "Read",
                LastName: "Only",
                Phone: null,
                PreferredCulture: "sv-SE",
                DefaultMarketId: Guid.Parse("62000000-0000-0000-0000-000000000001"),
                Status: "Active",
                IsGuest: false),
            CancellationToken.None);

        _ = await companyService.CreateMembershipAsync(
            new CreateCompanyMembershipCommand(
                Guid.Parse("77000000-0000-0000-0000-000000000001"),
                customer.Id,
                "Approver",
                "Active",
                false,
                false,
                true,
                false,
                null,
                null),
            CancellationToken.None);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            orderService.CreateAsync(
                new CreateOrderCommand(
                    CartId: null,
                    CartRowVersion: null,
                    CustomerId: customer.Id,
                    CompanyId: Guid.Parse("77000000-0000-0000-0000-000000000001"),
                    MarketId: Guid.Parse("62000000-0000-0000-0000-000000000001"),
                    CurrencyCode: "SEK",
                    CultureCode: "sv-SE",
                    Email: "readonly-buyer@example.com",
                    Lines:
                    [
                        new CreateOrderLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 1m, null)
                    ],
                    Addresses:
                    [
                        new CreateOrderAddressItem("Billing", "Read", "Only", "Northwind Construction", "Sveavagen 10", null, "11157", "Stockholm", null, "SE", "readonly-buyer@example.com", null)
                    ]),
                "tester",
                CancellationToken.None));
    }
}
