using Microsoft.Extensions.Options;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Infrastructure.Cart;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Attributes;
using Platform.Infrastructure.Catalog.Brands;
using Platform.Infrastructure.Catalog.Channels;
using Platform.Infrastructure.Catalog.Categories;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Media;
using Platform.Infrastructure.Catalog.Pricing;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Companies;
using Platform.Infrastructure.Customers;
using Platform.Infrastructure.Orders;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

public sealed class StorefrontCartApplicationServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPricedCartForStorefrontContext()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [new CreateStorefrontCartLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 2m, "Gift wrap")],
                [CreateBillingAddress()]),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Cart);
        Assert.Equal("SEK", result.Cart!.CurrencyCode);
        Assert.Equal("sv-SE", result.Cart.CultureCode);
        Assert.Equal("buyer@example.com", result.Cart.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Cart.CartAccessToken));
        Assert.Equal(2m, result.Cart.Lines[0].Quantity);
        Assert.Equal("SKU-EXAMPLE-1-BLACK", result.Cart.Lines[0].Sku);
        Assert.Equal("Example Drill", result.Cart.Lines[0].ProductName);
        Assert.Equal(2398.40m, result.Cart.Subtotal);
        Assert.Equal(599.60m, result.Cart.VatTotal);
        Assert.Equal(2998.00m, result.Cart.GrandTotal);
        Assert.Single(store.Carts.Values, x => x.Id == result.Cart.Id);
    }

    [Fact]
    public async Task CheckoutAsync_CreatesOrderAndConvertsCart()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);
        var created = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [new CreateStorefrontCartLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 1m, null)],
                CreateCheckoutAddresses()),
            CancellationToken.None);

        var checkout = await service.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, checkout.Status);
        Assert.NotNull(checkout.Order);
        Assert.Equal(created.Cart.Id, checkout.Order!.SourceCartId);
        Assert.Equal("Placed", checkout.Order.Status);
        Assert.Equal("Converted", store.Carts[created.Cart.Id].Status);
        Assert.Equal(1199.20m, checkout.Order.Subtotal);
        Assert.Equal(1499.00m, checkout.Order.GrandTotal);

        var secondCheckout = await service.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, secondCheckout.Status);
        Assert.Equal(checkout.Order.Id, secondCheckout.Order!.Id);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationFailureWhenCartHasNoLines()
    {
        var service = CreateService(new InMemoryCatalogStore());

        var result = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [],
                []),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, result.Status);
        Assert.Contains("Lines", result.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationFailureWhenVariantIsNotVisibleInStorefrontContext()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);
        store.StorefrontProductProjections.Clear();

        var result = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [new CreateStorefrontCartLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 1m, null)],
                [CreateBillingAddress()]),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Errors, x => x.Key.Contains("VariantId", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationFailureWhenQuantityExceedsProjectedAvailability()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [new CreateStorefrontCartLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 24m, null)],
                [CreateBillingAddress()]),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, result.Status);
        Assert.Contains(result.Errors.SelectMany(x => x.Value), x => x.Contains("has only 23", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CheckoutAsync_ReturnsValidationFailureWhenAddressesAreMissing()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);
        var created = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [new CreateStorefrontCartLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 1m, null)],
                [CreateBillingAddress()]),
            CancellationToken.None);

        var checkout = await service.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, checkout.Status);
        Assert.Contains("A shipping address is required before checkout.", checkout.Errors["Addresses"]);
        Assert.DoesNotContain(store.Orders.Values, x => x.SourceCartId == created.Cart.Id);
    }

    [Fact]
    public async Task GetByIdAsync_RequiresCartAccessToken()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);
        var created = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [new CreateStorefrontCartLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 1m, null)],
                [CreateBillingAddress()]),
            CancellationToken.None);

        var missingToken = await service.GetByIdAsync(
            new GetStorefrontCartByIdQuery(created.Cart!.Id, null),
            CancellationToken.None);
        var invalidToken = await service.GetByIdAsync(
            new GetStorefrontCartByIdQuery(created.Cart.Id, "not-a-valid-token"),
            CancellationToken.None);
        var validToken = await service.GetByIdAsync(
            new GetStorefrontCartByIdQuery(created.Cart.Id, created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Unauthorized, missingToken.Status);
        Assert.Equal(StorefrontContextResolutionStatus.Unauthorized, invalidToken.Status);
        Assert.Equal(StorefrontContextResolutionStatus.Success, validToken.Status);
    }

    [Fact]
    public async Task RepriceAndCheckoutAsync_RequireCartAccessToken()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);
        var created = await service.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                [new CreateStorefrontCartLineItem(Guid.Parse("50000000-0000-0000-0000-000000000011"), 1m, null)],
                CreateCheckoutAddresses()),
            CancellationToken.None);

        var reprice = await service.RepriceAsync(
            new RepriceStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, null),
            CancellationToken.None);
        var checkout = await service.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart.Id, created.Cart.RowVersion, "not-a-valid-token"),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Unauthorized, reprice.Status);
        Assert.Equal(StorefrontContextResolutionStatus.Unauthorized, checkout.Status);
        Assert.DoesNotContain(store.Orders.Values, x => x.SourceCartId == created.Cart.Id);
    }

    private static StorefrontCartApplicationService CreateService(InMemoryCatalogStore store)
    {
        SeedStorefrontProjections(store);
        var cartRepository = new InMemoryCartRepository(store);
        var orderRepository = new InMemoryOrderRepository(store);
        var cartPricingService = new CartAdminApplicationService(
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
            cartPricingService,
            new InMemoryUnitOfWork());
        var contextService = new StorefrontContextApplicationService(
            new InMemoryChannelRepository(store),
            new InMemoryMarketRepository(store));

        return new StorefrontCartApplicationService(
            cartRepository,
            contextService,
            new InMemoryStorefrontProductProjectionRepository(store),
            cartPricingService,
            orderService,
            new InMemoryUnitOfWork(),
            new StorefrontCartAccessTokenService(Options.Create(new StorefrontCartAccessTokenOptions
            {
                SigningKey = "test-storefront-cart-access-token-signing-key"
            })));
    }

    private static void SeedStorefrontProjections(InMemoryCatalogStore store)
    {
        var builder = new StorefrontProjectionBuilder(
            new InMemoryBrandRepository(store),
            new InMemoryCategoryRepository(store),
            new InMemoryInventoryBalanceRepository(store),
            new InMemoryInventoryLocationRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryMediaAssetRepository(store),
            new InMemoryPriceListRepository(store),
            new InMemoryProductAttributeRepository(store),
            new InMemoryProductRepository(store),
            new InMemoryVariantRepository(store));

        var projections = builder.BuildForProductAsync(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CancellationToken.None).GetAwaiter().GetResult();

        foreach (var projection in projections)
        {
            store.StorefrontProductProjections[projection.Id] = projection;
        }
    }

    private static IReadOnlyList<CreateStorefrontCartAddressItem> CreateCheckoutAddresses()
    {
        return [CreateBillingAddress(), CreateShippingAddress()];
    }

    private static CreateStorefrontCartAddressItem CreateBillingAddress()
    {
        return new CreateStorefrontCartAddressItem(
            "Billing",
            "Alicia",
            "Buyer",
            null,
            "Sveavagen 10",
            null,
            "11157",
            "Stockholm",
            null,
            "SE",
            "buyer@example.com",
            "+46 70 100 10 10");
    }

    private static CreateStorefrontCartAddressItem CreateShippingAddress()
    {
        return new CreateStorefrontCartAddressItem(
            "Shipping",
            "Alicia",
            "Buyer",
            null,
            "Sveavagen 10",
            null,
            "11157",
            "Stockholm",
            null,
            "SE",
            "buyer@example.com",
            "+46 70 100 10 10");
    }
}
