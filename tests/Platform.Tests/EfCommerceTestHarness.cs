using Microsoft.EntityFrameworkCore;
using Platform.Application.Abstractions.Persistence;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Pricing;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Tax;
using Platform.Domain.Catalog.Variants;
using Platform.Infrastructure.Cart;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Pricing;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Tax;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Companies;
using Platform.Infrastructure.Customers;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Orders;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Storefront;
using CartEntity = Platform.Domain.Cart.Cart;

namespace Platform.Tests;

/// <summary>
/// Seeds a minimal sellable catalog (market, VAT rule, price list, stock location) through EF and
/// builds the real order and inventory services over one <see cref="PlatformDbContext"/>. PostgreSQL
/// only: the EF in-memory provider has no transactions, so a rejected save can leave partial rows.
/// </summary>
internal sealed class EfCommerceTestHarness
{
    private readonly Func<PlatformDbContext> _createContext;

    public EfCommerceTestHarness(Func<PlatformDbContext> createContext)
    {
        _createContext = createContext;
    }

    public Guid MarketId { get; private set; }
    public Guid LocationId { get; private set; }

    public static EfCommerceTestHarness ForPostgres(string connectionString)
    {
        return new EfCommerceTestHarness(() => PostgresTestDatabase.CreateContext(connectionString));
    }

    public PlatformDbContext CreateContext()
    {
        return _createContext();
    }

    public async Task SeedMarketAsync()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var market = new Market(Guid.NewGuid(), "SE", "Sweden", "SEK", "sv-SE", "Gross", now, now);
        db.Markets.Add(market);
        db.VatRules.Add(new VatRule(Guid.NewGuid(), market.Id, "STANDARD", 0.25m, null, now, now));
        var location = new InventoryLocation(Guid.NewGuid(), "MAIN", "Main", "Warehouse", "SE", now, now);
        location.UpsertMarketAssignment(market.Id, 0, location.RowVersion);
        db.InventoryLocations.Add(location);
        var priceList = new PriceList(Guid.NewGuid(), "SE_BASE", "SE Base", "SEK", true, null, null, now, now);
        priceList.UpsertMarketAssignment(market.Id, 0, true, priceList.RowVersion);
        db.PriceLists.Add(priceList);
        await db.SaveChangesAsync();
        MarketId = market.Id;
        LocationId = location.Id;
    }

    /// <summary>Adds a priced variant (125 gross) with one balance at the main location.</summary>
    public async Task<(Guid VariantId, Guid BalanceId)> SeedVariantAsync(string sku, decimal onHand, bool backorderable = false)
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var productStatus = await GetOrAddStatusAsync(db, ProductStatusEntityType.Product);
        var variantStatus = await GetOrAddStatusAsync(db, ProductStatusEntityType.Variant);
        var product = new Product(Guid.NewGuid(), "Hardware", $"P-{sku}", $"p-{sku.ToLowerInvariant()}", null, productStatus, "STANDARD", "pcs", true, [], [], null, null, null, null, now, now);
        product.UpsertTranslation("en-GB", $"Product {sku}", null, null, null, null);
        var variant = new Variant(Guid.NewGuid(), product.Id, sku, null, null, null, variantStatus, true, null, null, null, null, now, now, []);
        db.Products.Add(product);
        db.Variants.Add(variant);

        var priceList = await db.PriceLists.Include(x => x.Entries).SingleAsync(x => x.Code == "SE_BASE");
        priceList.UpsertEntry(null, "Variant", variant.Id, 1, 125m, null, null, null, priceList.RowVersion);

        var balance = new InventoryBalance(Guid.NewGuid(), LocationId, variant.Id, onHand, 0m, 0m, backorderable, now);
        db.InventoryBalances.Add(balance);
        await db.SaveChangesAsync();
        return (variant.Id, balance.Id);
    }

    public async Task<(Guid CartId, string RowVersion)> SeedCartAsync(params (Guid VariantId, decimal Quantity)[] lines)
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var cart = new CartEntity(Guid.NewGuid(), null, null, MarketId, "SEK", "sv-SE", "buyer@example.com", now.AddDays(1), now, now);
        foreach (var (variantId, quantity) in lines)
        {
            cart.AddLine(variantId, variantId.ToString(), "Item", null, quantity, 0m, 0m, null);
        }

        cart.AddAddress("Shipping", "Alicia", "Buyer", null, "Sveavagen 10", null, "11157", "Stockholm", null, "SE", "buyer@example.com", null);
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
        return (cart.Id, cart.RowVersion);
    }

    public static OrderAdminApplicationService CreateOrders(PlatformDbContext db, IUnitOfWork? unitOfWork = null)
    {
        unitOfWork ??= new EfUnitOfWork(db);
        var cartRepository = new EfCartRepository(db);
        var pricing = new CartAdminApplicationService(
            cartRepository,
            new EfPriceListRepository(db),
            new EfVariantRepository(db),
            new EfProductRepository(db),
            new EfVatRuleRepository(db),
            new EfMarketRepository(db),
            unitOfWork);
        return new OrderAdminApplicationService(
            new EfOrderRepository(db),
            cartRepository,
            new EfCompanyRepository(db),
            new EfCustomerRepository(db),
            new EfMarketRepository(db),
            pricing,
            CreateReservations(db),
            new EfInventoryWriteCoordinator(db),
            unitOfWork);
    }

    public static InventoryAdminApplicationService CreateInventory(PlatformDbContext db, IUnitOfWork? unitOfWork = null)
    {
        return new InventoryAdminApplicationService(
            new EfInventoryLocationRepository(db),
            new EfInventoryBalanceRepository(db),
            new EfInventoryAllocationRepository(db),
            new EfInventoryWriteCoordinator(db),
            new EfMarketRepository(db),
            new EfVariantRepository(db),
            RefreshPublisher(db),
            unitOfWork ?? new EfUnitOfWork(db));
    }

    private static InventoryReservationService CreateReservations(PlatformDbContext db)
    {
        return new InventoryReservationService(
            new EfInventoryLocationRepository(db),
            new EfInventoryBalanceRepository(db),
            new EfInventoryAllocationRepository(db),
            new EfMarketRepository(db),
            RefreshPublisher(db),
            TimeProvider.System);
    }

    private static StorefrontProjectionRefreshRequestPublisher RefreshPublisher(PlatformDbContext db)
    {
        return new StorefrontProjectionRefreshRequestPublisher(new OutboxEventPublisher(new EfOutboxMessageRepository(db)));
    }

    private static async Task<ProductStatusDefinition> GetOrAddStatusAsync(PlatformDbContext db, ProductStatusEntityType entityType)
    {
        // PostgreSQL databases get READY statuses from the SeedCatalogStatuses migration.
        var existing = await db.ProductStatusDefinitions.FirstOrDefaultAsync(x => x.EntityType == entityType && x.Code == "READY");
        if (existing is not null)
        {
            return existing;
        }

        var status = new ProductStatusDefinition(Guid.NewGuid(), entityType, "READY", "Ready", true);
        var entry = db.ProductStatusDefinitions.Add(status);
        entry.Property("Status").CurrentValue = "Active";
        entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToString("N");
        entry.Property("CreatedAtUtc").CurrentValue = DateTime.UtcNow;
        entry.Property("UpdatedAtUtc").CurrentValue = DateTime.UtcNow;
        return status;
    }
}

/// <summary>
/// Runs a callback once, immediately before the first save, so a competing operation can commit
/// between this operation's reads and its write.
/// </summary>
internal sealed class InterleavingUnitOfWork : IUnitOfWork
{
    private readonly IUnitOfWork _inner;
    private Func<Task>? _beforeFirstSave;

    public InterleavingUnitOfWork(IUnitOfWork inner, Func<Task> beforeFirstSave)
    {
        _inner = inner;
        _beforeFirstSave = beforeFirstSave;
    }

    public int SaveAttempts { get; private set; }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveAttempts++;
        var callback = _beforeFirstSave;
        _beforeFirstSave = null;
        if (callback is not null)
        {
            await callback();
        }

        await _inner.SaveChangesAsync(cancellationToken);
    }
}
