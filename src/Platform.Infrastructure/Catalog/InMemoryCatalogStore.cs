using System.Collections.Concurrent;
using CartEntity = Platform.Domain.Cart.Cart;
using Platform.Domain.Auditing;
using Platform.Domain.Catalog.Attributes;
using Platform.Domain.Catalog.Brands;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Catalog.Channels;
using Platform.Domain.Catalog.Categories;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Media;
using Platform.Domain.Catalog.Pricing;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;
using Platform.Domain.Companies;
using Platform.Domain.Customers;
using Platform.Domain.Integrations;
using Platform.Domain.Orders;
using Platform.Domain.Security;
using Platform.Application.Storefront;

namespace Platform.Infrastructure.Catalog;

public sealed class InMemoryCatalogStore
{
    public InMemoryCatalogStore()
    {
        Seed();
    }

    public ConcurrentDictionary<Guid, Product> Products { get; } = new();
    public ConcurrentDictionary<Guid, Variant> Variants { get; } = new();
    public ConcurrentDictionary<Guid, AuditLog> AuditLogs { get; } = new();
    public ConcurrentDictionary<Guid, AdminUser> AdminUsers { get; } = new();
    public ConcurrentDictionary<Guid, Brand> Brands { get; } = new();
    public ConcurrentDictionary<Guid, IntegrationJob> IntegrationJobs { get; } = new();
    public ConcurrentDictionary<Guid, OutboxMessage> OutboxMessages { get; } = new();
    public ConcurrentDictionary<Guid, InventoryBalance> InventoryBalances { get; } = new();
    public ConcurrentDictionary<Guid, InventoryLocation> InventoryLocations { get; } = new();
    public ConcurrentDictionary<Guid, InventoryTransaction> InventoryTransactions { get; } = new();
    public ConcurrentDictionary<Guid, Market> Markets { get; } = new();
    public ConcurrentDictionary<Guid, Customer> Customers { get; } = new();
    public ConcurrentDictionary<Guid, Company> Companies { get; } = new();
    public ConcurrentDictionary<Guid, CartEntity> Carts { get; } = new();
    public ConcurrentDictionary<Guid, Channel> Channels { get; } = new();
    public ConcurrentDictionary<Guid, Category> Categories { get; } = new();
    public ConcurrentDictionary<Guid, MediaAsset> MediaAssets { get; } = new();
    public ConcurrentDictionary<Guid, Order> Orders { get; } = new();
    public ConcurrentDictionary<Guid, PriceList> PriceLists { get; } = new();
    public ConcurrentDictionary<Guid, ProductAttribute> ProductAttributes { get; } = new();
    public ConcurrentDictionary<Guid, StorefrontProductProjection> StorefrontProductProjections { get; } = new();
    public ConcurrentDictionary<Guid, WebhookSubscription> WebhookSubscriptions { get; } = new();
    public ConcurrentDictionary<Guid, WebhookDelivery> WebhookDeliveries { get; } = new();

    public IReadOnlyDictionary<Guid, ProductStatusDefinition> Statuses { get; } =
        new Dictionary<Guid, ProductStatusDefinition>
        {
            [Guid.Parse("10000000-0000-0000-0000-000000000001")] = new(Guid.Parse("10000000-0000-0000-0000-000000000001"), ProductStatusEntityType.Product, "DRAFT", "Draft", false),
            [Guid.Parse("10000000-0000-0000-0000-000000000002")] = new(Guid.Parse("10000000-0000-0000-0000-000000000002"), ProductStatusEntityType.Product, "READY", "Ready", true),
            [Guid.Parse("10000000-0000-0000-0000-000000000003")] = new(Guid.Parse("10000000-0000-0000-0000-000000000003"), ProductStatusEntityType.Product, "COMING_SOON", "Coming Soon", false),
            [Guid.Parse("10000000-0000-0000-0000-000000000004")] = new(Guid.Parse("10000000-0000-0000-0000-000000000004"), ProductStatusEntityType.Product, "DISCONTINUED", "Discontinued", false),
            [Guid.Parse("10000000-0000-0000-0000-000000000101")] = new(Guid.Parse("10000000-0000-0000-0000-000000000101"), ProductStatusEntityType.Variant, "DRAFT", "Draft", false),
            [Guid.Parse("10000000-0000-0000-0000-000000000102")] = new(Guid.Parse("10000000-0000-0000-0000-000000000102"), ProductStatusEntityType.Variant, "READY", "Ready", true),
            [Guid.Parse("10000000-0000-0000-0000-000000000103")] = new(Guid.Parse("10000000-0000-0000-0000-000000000103"), ProductStatusEntityType.Variant, "COMING_SOON", "Coming Soon", false),
            [Guid.Parse("10000000-0000-0000-0000-000000000104")] = new(Guid.Parse("10000000-0000-0000-0000-000000000104"), ProductStatusEntityType.Variant, "DISCONTINUED", "Discontinued", false)
        };

    private void Seed()
    {
        var readyProductStatus = Statuses[Guid.Parse("10000000-0000-0000-0000-000000000002")];
        var readyVariantStatus = Statuses[Guid.Parse("10000000-0000-0000-0000-000000000102")];
        var now = DateTime.UtcNow;

        var drillHero = new MediaAsset(
            Guid.Parse("74000000-0000-0000-0000-000000000001"),
            "External",
            "https://images.example.com/drill-hero.jpg",
            "drill-hero.jpg",
            "image/jpeg",
            0,
            1200,
            1200,
            "https://images.example.com/drill-hero.jpg",
            "Example drill hero image",
            "Example Drill",
            now.AddMinutes(-33),
            now.AddMinutes(-10));
        MediaAssets[drillHero.Id] = drillHero;

        var acmeBrand = new Brand(
            Guid.Parse("61000000-0000-0000-0000-000000000001"),
            "ACME",
            "https://www.example.com",
            drillHero.Id,
            10,
            now.AddMinutes(-41),
            now.AddMinutes(-10));
        acmeBrand.UpsertTranslation("en-GB", "Acme Tools", "acme-tools", "Sample brand for seeded catalog data.");
        Brands[acmeBrand.Id] = acmeBrand;

        var seMarket = new Market(
            Guid.Parse("62000000-0000-0000-0000-000000000001"),
            "SE",
            "Sweden",
            "SEK",
            "sv-SE",
            "Gross",
            now.AddMinutes(-41),
            now.AddMinutes(-9));
        Markets[seMarket.Id] = seMarket;

        var customer = new Customer(
            Guid.Parse("76000000-0000-0000-0000-000000000001"),
            "crm-1001",
            "user-1001",
            "buyer@example.com",
            "Alicia",
            "Buyer",
            "+46 70 100 10 10",
            "en-GB",
            seMarket.Id,
            "Active",
            false,
            now.AddMinutes(-22),
            now.AddMinutes(-9));
        customer.AddAddress(
            "Shipping",
            null,
            "Alicia",
            "Buyer",
            "Northwind Construction",
            "Sveavagen 10",
            null,
            "11157",
            "Stockholm",
            null,
            "SE",
            "+46 70 100 10 10",
            "buyer@example.com",
            true);
        Customers[customer.Id] = customer;

        var company = new Company(
            Guid.Parse("77000000-0000-0000-0000-000000000001"),
            "erp-2001",
            "NORTHWIND",
            "Northwind Construction",
            "Northwind Construction AB",
            "556677-8899",
            "SE556677889901",
            "orders@northwind.example",
            "+46 8 100 20 30",
            seMarket.Id,
            "SEK",
            "Active",
            now.AddMinutes(-22),
            now.AddMinutes(-9));
        company.AddAddress(
            "Billing",
            "Accounts Payable",
            "Sveavagen 10",
            null,
            "11157",
            "Stockholm",
            null,
            "SE",
            "ap@northwind.example",
            "+46 8 100 20 31",
            true);
        company.AddMembership(
            customer.Id,
            "Buyer",
            "Active",
            true,
            true,
            false,
            true,
            now.AddDays(-30),
            null);
        Companies[company.Id] = company;

        var webSeChannel = new Channel(
            Guid.Parse("63000000-0000-0000-0000-000000000001"),
            "WEB-SE",
            "Swedish Web",
            "se.example.com",
            now.AddMinutes(-41),
            now.AddMinutes(-9));
        webSeChannel.UpsertMarketAssignment(seMarket.Id, webSeChannel.RowVersion);
        Channels[webSeChannel.Id] = webSeChannel;

        var toolsCategory = new Category(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            "TOOLS",
            null,
            10,
            now.AddMinutes(-40),
            now.AddMinutes(-12));
        toolsCategory.UpsertTranslation("en-GB", "Tools", "tools", "Catalog root for tools.");
        Categories[toolsCategory.Id] = toolsCategory;

        var drillsCategory = new Category(
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            "DRILLS",
            toolsCategory.Id,
            20,
            now.AddMinutes(-39),
            now.AddMinutes(-11));
        drillsCategory.UpsertTranslation("en-GB", "Drills", "drills", "Electric and battery-powered drills.");
        Categories[drillsCategory.Id] = drillsCategory;

        var colorAttribute = new ProductAttribute(
            Guid.Parse("71000000-0000-0000-0000-000000000001"),
            "COLOR",
            "Color",
            "Variant",
            "Select",
            true,
            true,
            true,
            10,
            now.AddMinutes(-35),
            now.AddMinutes(-12),
            [
                new AttributeOption(Guid.Parse("72000000-0000-0000-0000-000000000001"), "BLACK", "Black", 10),
                new AttributeOption(Guid.Parse("72000000-0000-0000-0000-000000000002"), "RED", "Red", 20)
            ]);
        ProductAttributes[colorAttribute.Id] = colorAttribute;

        var powerSourceAttribute = new ProductAttribute(
            Guid.Parse("71000000-0000-0000-0000-000000000002"),
            "POWER_SOURCE",
            "Power Source",
            "Product",
            "Select",
            false,
            true,
            true,
            10,
            now.AddMinutes(-34),
            now.AddMinutes(-10),
            [
                new AttributeOption(Guid.Parse("72000000-0000-0000-0000-000000000011"), "CORDED", "Corded", 10),
                new AttributeOption(Guid.Parse("72000000-0000-0000-0000-000000000012"), "CORDLESS", "Cordless", 20)
            ]);
        ProductAttributes[powerSourceAttribute.Id] = powerSourceAttribute;

        var product = new Product(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            "Hardware",
            "SKU-EXAMPLE-1",
            "example-drill",
            acmeBrand.Id,
            readyProductStatus,
            "STANDARD",
            "pcs",
            true,
            [drillsCategory.Id],
            [
                new ProductAttributeValue(
                    Guid.Parse("73000000-0000-0000-0000-000000000011"),
                    powerSourceAttribute.Id,
                    Guid.Parse("72000000-0000-0000-0000-000000000011"),
                    null)
            ],
            1.8m,
            28.0m,
            8.0m,
            22.0m,
            now.AddMinutes(-30),
            now.AddMinutes(-10));

        product.UpsertTranslation(
            "en-GB",
            "Example Drill",
            "Compact and powerful drill for demanding work.",
            "A compact drill designed for demanding work and reliable day-to-day use.",
            "Example Drill | Demo",
            "Compact and powerful drill for demanding work.");
        product.UpsertMedia(drillHero.Id, "Image", 10, true, product.RowVersion);
        seMarket.UpsertProductAssignment(product.Id, "Active", seMarket.RowVersion);

        Products[product.Id] = product;

        var variant = new Variant(
            Guid.Parse("50000000-0000-0000-0000-000000000011"),
            product.Id,
            "SKU-EXAMPLE-1-BLACK",
            "1234567890123",
            "ACME-DRILL-BLK",
            "1234567890123",
            readyVariantStatus,
            true,
            1.8m,
            28.0m,
            8.0m,
            22.0m,
            now.AddMinutes(-25),
            now.AddMinutes(-10),
            [
                new VariantAttributeValue(
                    Guid.Parse("73000000-0000-0000-0000-000000000001"),
                    colorAttribute.Id,
                    Guid.Parse("72000000-0000-0000-0000-000000000001"),
                    null)
            ]);
        variant.UpsertMedia(drillHero.Id, "Image", 10, true, variant.RowVersion);

        Variants[variant.Id] = variant;

        var seBasePriceList = new PriceList(
            Guid.Parse("64000000-0000-0000-0000-000000000001"),
            "SE_BASE_GROSS",
            "SE Base Gross",
            "SEK",
            true,
            null,
            null,
            now.AddMinutes(-20),
            now.AddMinutes(-9));
        seBasePriceList.UpsertMarketAssignment(seMarket.Id, 0, true, seBasePriceList.RowVersion);
        seBasePriceList.UpsertEntry(null, "Variant", variant.Id, 1, 1499m, 1699m, null, null, seBasePriceList.RowVersion);
        PriceLists[seBasePriceList.Id] = seBasePriceList;

        var mainLocation = new InventoryLocation(
            Guid.Parse("65000000-0000-0000-0000-000000000001"),
            "MAIN",
            "Main Warehouse",
            "Warehouse",
            "SE",
            now.AddMinutes(-20),
            now.AddMinutes(-9));
        mainLocation.UpsertMarketAssignment(seMarket.Id, 0, mainLocation.RowVersion);
        InventoryLocations[mainLocation.Id] = mainLocation;

        var balance = new InventoryBalance(
            Guid.Parse("66000000-0000-0000-0000-000000000001"),
            mainLocation.Id,
            variant.Id,
            25m,
            2m,
            10m,
            false,
            now.AddMinutes(-8));
        InventoryBalances[balance.Id] = balance;

        var transaction = new InventoryTransaction(
            Guid.Parse("67000000-0000-0000-0000-000000000001"),
            mainLocation.Id,
            variant.Id,
            "Adjustment",
            25m,
            "Seed",
            Guid.Parse("68000000-0000-0000-0000-000000000001"),
            now.AddMinutes(-8));
        InventoryTransactions[transaction.Id] = transaction;

        var cart = new CartEntity(
            Guid.Parse("78000000-0000-0000-0000-000000000001"),
            customer.Id,
            company.Id,
            seMarket.Id,
            "SEK",
            "sv-SE",
            "buyer@example.com",
            now.AddDays(7),
            now.AddMinutes(-7),
            now.AddMinutes(-7));
        cart.AddLine(
            variant.Id,
            variant.Sku,
            "Example Drill",
            "Black",
            2m,
            1499m / 1.25m,
            0.25m,
            "Seed cart");
        cart.AddAddress(
            "Shipping",
            customer.FirstName,
            customer.LastName,
            company.Name,
            "Sveavagen 10",
            null,
            "11157",
            "Stockholm",
            null,
            "SE",
            "buyer@example.com",
            customer.Phone);
        Carts[cart.Id] = cart;

        var orderId = Guid.Parse("79000000-0000-0000-0000-000000000001");
        var order = new Order(
            orderId,
            null,
            "ORD-202604230001",
            customer.Id,
            company.Id,
            seMarket.Id,
            "SEK",
            "sv-SE",
            "buyer@example.com",
            now.AddMinutes(-5),
            [
                new OrderLine(
                    Guid.Parse("79000000-0000-0000-0000-000000000011"),
                    orderId,
                    variant.Id,
                    variant.Sku,
                    "Example Drill",
                    "Black",
                    1m,
                    1499m / 1.25m,
                    0.25m)
            ],
            [
                new OrderAddress(
                    Guid.Parse("79000000-0000-0000-0000-000000000021"),
                    orderId,
                    "Billing",
                    customer.FirstName,
                    customer.LastName,
                    company.Name,
                    "Sveavagen 10",
                    null,
                    "11157",
                    "Stockholm",
                    null,
                    "SE",
                    "buyer@example.com",
                    customer.Phone)
            ],
            "seed",
            "Seed order.");
        var processingHistory = order.ChangeStatus("Processing", "seed", "Seed processing update.", order.RowVersion);
        _ = processingHistory;
        Orders[order.Id] = order;
    }
}
