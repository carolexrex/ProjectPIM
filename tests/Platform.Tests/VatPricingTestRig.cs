using Microsoft.Extensions.Options;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Pricing;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Tax;
using Platform.Domain.Catalog.Variants;
using Platform.Infrastructure.Cart;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Pricing;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Channels;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Tax;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Companies;
using Platform.Infrastructure.Customers;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Orders;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

/// <summary>
/// In-memory commerce rig used by the VAT and inventory reservation tests. It extends the seeded SE
/// catalog with extra products, tax categories, price lists, markets, and stock locations, and wires
/// the real pricing, order, storefront cart, VAT rule, and inventory services over one store.
/// </summary>
internal sealed class VatPricingTestRig
{
    public static readonly Guid SeMarketId = Guid.Parse("62000000-0000-0000-0000-000000000001");
    public static readonly Guid SeStandardVatRuleId = Guid.Parse("64100000-0000-0000-0000-000000000001");
    public static readonly Guid SeededProductId = StorefrontProjectionBuilderTests.ProductId;
    public static readonly Guid SeededVariantId = StorefrontProjectionBuilderTests.VariantId;
    public static readonly Guid SeBasePriceListId = StorefrontProjectionBuilderTests.BasePriceListId;
    public static readonly Guid MainLocationId = Guid.Parse("65000000-0000-0000-0000-000000000001");

    public VatPricingTestRig()
    {
        Store = new InMemoryCatalogStore();
        var cartRepository = new InMemoryCartRepository(Store);
        CartPricing = new CartAdminApplicationService(
            cartRepository,
            new InMemoryPriceListRepository(Store),
            new InMemoryVariantRepository(Store),
            new InMemoryProductRepository(Store),
            new InMemoryVatRuleRepository(Store),
            new InMemoryMarketRepository(Store),
            new InMemoryUnitOfWork());
        Orders = new OrderAdminApplicationService(
            new InMemoryOrderRepository(Store),
            cartRepository,
            new InMemoryCompanyRepository(Store),
            new InMemoryCustomerRepository(Store),
            new InMemoryMarketRepository(Store),
            CartPricing,
            InventoryTestServices.CreateReservationService(Store),
            InventoryTestServices.CreateCoordinator(Store),
            new InMemoryUnitOfWork());
        Inventory = new InventoryAdminApplicationService(
            new InMemoryInventoryLocationRepository(Store),
            new InMemoryInventoryBalanceRepository(Store),
            new InMemoryInventoryAllocationRepository(Store),
            InventoryTestServices.CreateCoordinator(Store),
            new InMemoryMarketRepository(Store),
            new InMemoryVariantRepository(Store),
            new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new InMemoryOutboxMessageRepository(Store))),
            new InMemoryUnitOfWork());
        VatRules = new VatRuleAdminApplicationService(
            new InMemoryVatRuleRepository(Store),
            new InMemoryMarketRepository(Store),
            new InMemoryUnitOfWork());
        Storefront = new StorefrontCartApplicationService(
            cartRepository,
            new StorefrontContextApplicationService(new InMemoryChannelRepository(Store), new InMemoryMarketRepository(Store)),
            new InMemoryStorefrontProductProjectionRepository(Store),
            CartPricing,
            Orders,
            new InMemoryUnitOfWork(),
            new StorefrontCartAccessTokenService(Options.Create(new StorefrontCartAccessTokenOptions
            {
                SigningKey = "test-storefront-cart-access-token-signing-key"
            })));
    }

    public InMemoryCatalogStore Store { get; }
    public CartAdminApplicationService CartPricing { get; }
    public OrderAdminApplicationService Orders { get; }
    public VatRuleAdminApplicationService VatRules { get; }
    public InventoryAdminApplicationService Inventory { get; }
    public StorefrontCartApplicationService Storefront { get; }

    public PriceList SeBasePriceList => Store.PriceLists[SeBasePriceListId];

    public VatRule AddVatRule(Guid marketId, string taxCategoryCode, decimal rate)
    {
        var now = DateTime.UtcNow;
        var rule = new VatRule(Guid.NewGuid(), marketId, taxCategoryCode, rate, null, now, now);
        Store.VatRules[rule.Id] = rule;
        return rule;
    }

    /// <summary>
    /// Adds a visible, stocked product in the SE market with one variant and returns the variant id.
    /// The variant is priced on <paramref name="priceList"/> (SE base gross list by default).
    /// </summary>
    public Guid AddProduct(int index, string taxCategoryCode, decimal amount, PriceList? priceList = null)
    {
        var template = Store.Products[SeededProductId];
        var templateVariant = Store.Variants[SeededVariantId];
        var createdAtUtc = DateTime.UtcNow.AddDays(-1);

        var product = new Product(
            Guid.Parse($"51000000-0000-0000-0000-{index:D12}"),
            template.ProductType,
            $"SKU-VAT-{index}",
            $"vat-product-{index}",
            template.BrandId,
            template.ProductStatus,
            taxCategoryCode,
            template.UnitOfMeasure,
            true,
            template.CategoryAssignments.Select(x => x.CategoryId).ToList(),
            [],
            null,
            null,
            null,
            null,
            createdAtUtc,
            createdAtUtc);
        product.UpsertTranslation("en-GB", $"VAT Product {index}", null, null, null, null);
        Store.Products[product.Id] = product;

        var variant = new Variant(
            Guid.Parse($"51000000-0000-0000-0001-{index:D12}"),
            product.Id,
            $"SKU-VAT-{index}-STD",
            null,
            null,
            null,
            templateVariant.ProductStatus,
            true,
            null,
            null,
            null,
            null,
            createdAtUtc,
            createdAtUtc,
            []);
        Store.Variants[variant.Id] = variant;

        var market = Store.Markets[SeMarketId];
        market.UpsertProductAssignment(product.Id, "Active", market.RowVersion);

        var balance = new InventoryBalance(Guid.NewGuid(), MainLocationId, variant.Id, 1000m, 0m, 0m, false, createdAtUtc);
        Store.InventoryBalances[balance.Id] = balance;

        var targetList = priceList ?? SeBasePriceList;
        targetList.UpsertEntry(null, "Variant", variant.Id, 1, amount, null, null, null, targetList.RowVersion);
        return variant.Id;
    }

    public PriceList AddPriceList(string code, string currencyCode, bool vatIncluded, Guid marketId, int priority)
    {
        var now = DateTime.UtcNow;
        var priceList = new PriceList(Guid.NewGuid(), code, code, currencyCode, vatIncluded, null, null, now, now);
        priceList.UpsertMarketAssignment(marketId, priority, false, priceList.RowVersion);
        Store.PriceLists[priceList.Id] = priceList;
        return priceList;
    }

    public Market AddMarket(Guid id, string code, string currencyCode, string cultureCode)
    {
        var now = DateTime.UtcNow;
        var market = new Market(id, code, code, currencyCode, cultureCode, "Gross", now, now);
        Store.Markets[market.Id] = market;
        return market;
    }

    /// <summary>
    /// Adds an active inventory location assigned to <paramref name="marketId"/> at <paramref name="priority"/>.
    /// </summary>
    public InventoryLocation AddLocation(Guid marketId, string code, int priority = 0)
    {
        var now = DateTime.UtcNow;
        var location = new InventoryLocation(Guid.NewGuid(), code, code, "Warehouse", null, now, now);
        location.UpsertMarketAssignment(marketId, priority, location.RowVersion);
        Store.InventoryLocations[location.Id] = location;
        return location;
    }

    public InventoryBalance AddBalance(Guid locationId, Guid variantId, decimal onHand, decimal reserved = 0m, bool backorderable = false)
    {
        var balance = new InventoryBalance(Guid.NewGuid(), locationId, variantId, onHand, reserved, 0m, backorderable, DateTime.UtcNow);
        Store.InventoryBalances[balance.Id] = balance;
        return balance;
    }

    /// <summary>
    /// Rebuilds storefront projections for the seeded product and every product added to the rig,
    /// replacing earlier projections the way the refresh worker does.
    /// </summary>
    public void Project()
    {
        var builder = StorefrontProjectionBuilderTests.CreateBuilder(Store);
        var repository = new InMemoryStorefrontProductProjectionRepository(Store);
        foreach (var productId in Store.Products.Keys.ToList())
        {
            var projections = builder.BuildForProductAsync(productId, CancellationToken.None).GetAwaiter().GetResult();
            repository.ReplaceForProductAsync(productId, projections, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    public Task<StorefrontCartResult> CreateStorefrontCartAsync(params (Guid VariantId, decimal Quantity)[] lines)
    {
        return Storefront.CreateAsync(
            new CreateStorefrontCartCommand(
                "WEB-SE",
                "SE",
                "sv-SE",
                "SEK",
                null,
                "buyer@example.com",
                lines.Select(x => new CreateStorefrontCartLineItem(x.VariantId, x.Quantity, null)).ToList(),
                [CreateAddress("Billing"), CreateAddress("Shipping")]),
            CancellationToken.None);
    }

    public static CreateStorefrontCartAddressItem CreateAddress(string type)
    {
        return new CreateStorefrontCartAddressItem(
            type,
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
