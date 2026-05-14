using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Abstractions.Security;
using CartEntity = Platform.Domain.Cart.Cart;
using Platform.Domain.Auditing;
using Platform.Domain.Cart;
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

namespace Platform.Infrastructure.Persistence;

public sealed class PlatformDbContext : DbContext, IUnitOfWork
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<Type> AuditedEntityTypes =
    [
        typeof(AdminUser),
        typeof(Brand),
        typeof(Category),
        typeof(Channel),
        typeof(Company),
        typeof(CompanyMembership),
        typeof(Customer),
        typeof(CartEntity),
        typeof(InventoryBalance),
        typeof(InventoryLocation),
        typeof(InventoryTransaction),
        typeof(IntegrationJob),
        typeof(Market),
        typeof(MediaAsset),
        typeof(Order),
        typeof(WebhookSubscription),
        typeof(PriceList),
        typeof(Product),
        typeof(ProductAttribute),
        typeof(Variant)
    ];
    private static readonly HashSet<string> ExcludedAuditPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAtUtc",
        "UpdatedAtUtc",
        "RowVersion",
        "NormalizedEmail",
        "NormalizedUsername",
        "PasswordHash",
        "IsDeleted",
        "TenantId"
    };

    private readonly ICurrentActorAccessor _currentActorAccessor;

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options, ICurrentActorAccessor currentActorAccessor)
        : base(options)
    {
        _currentActorAccessor = currentActorAccessor;
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminUserRoleAssignment> AdminUserRoles => Set<AdminUserRoleAssignment>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<IntegrationJob> IntegrationJobs => Set<IntegrationJob>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<BrandTranslation> BrandTranslations => Set<BrandTranslation>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryLocation> InventoryLocations => Set<InventoryLocation>();
    public DbSet<InventoryLocationMarketAssignment> InventoryLocationMarketAssignments => Set<InventoryLocationMarketAssignment>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMarketAssignment> ChannelMarketAssignments => Set<ChannelMarketAssignment>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<MarketCurrency> MarketCurrencies => Set<MarketCurrency>();
    public DbSet<MarketCulture> MarketCultures => Set<MarketCulture>();
    public DbSet<MarketProductAssignment> MarketProductAssignments => Set<MarketProductAssignment>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListEntry> PriceListEntries => Set<PriceListEntry>();
    public DbSet<PriceListMarketAssignment> PriceListMarketAssignments => Set<PriceListMarketAssignment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyAddress> CompanyAddresses => Set<CompanyAddress>();
    public DbSet<CompanyMembership> CompanyMemberships => Set<CompanyMembership>();
    public DbSet<CartEntity> Carts => Set<CartEntity>();
    public DbSet<CartLine> CartLines => Set<CartLine>();
    public DbSet<CartAddress> CartAddresses => Set<CartAddress>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderAddress> OrderAddresses => Set<OrderAddress>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<ProductCategoryAssignment> ProductCategoryAssignments => Set<ProductCategoryAssignment>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();
    public DbSet<ProductRelation> ProductRelations => Set<ProductRelation>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<ProductStatusDefinition> ProductStatusDefinitions => Set<ProductStatusDefinition>();
    public DbSet<StorefrontProductProjection> StorefrontProductProjections => Set<StorefrontProductProjection>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<VariantAttributeValue> VariantAttributeValues => Set<VariantAttributeValue>();
    public DbSet<VariantMedia> VariantMedia => Set<VariantMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        AppendAuditLogs(DateTime.UtcNow);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        AppendAuditLogs(DateTime.UtcNow);
        return base.SaveChangesAsync(cancellationToken);
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        _ = await SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Properties.Any(x => x.Metadata.Name == "UpdatedAtUtc"))
            {
                entry.Property("UpdatedAtUtc").CurrentValue = now;
            }

            if (entry.State == EntityState.Added && entry.Properties.Any(x => x.Metadata.Name == "CreatedAtUtc"))
            {
                var createdAt = entry.Property("CreatedAtUtc").CurrentValue;
                if (createdAt is null || createdAt is DateTime dateTime && dateTime == default)
                {
                    entry.Property("CreatedAtUtc").CurrentValue = now;
                }
            }
        }
    }

    private void AppendAuditLogs(DateTime now)
    {
        var actor = _currentActorAccessor.GetCurrentActor();
        var entries = ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => entry.Entity is not AuditLog and not AdminUserRoleAssignment)
            .Where(entry => entry.Metadata.ClrType is not null && AuditedEntityTypes.Contains(entry.Metadata.ClrType))
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var auditLogs = new List<AuditLog>(entries.Count);
        foreach (var entry in entries)
        {
            var entityId = TryReadEntityId(entry);
            if (string.IsNullOrWhiteSpace(entityId))
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => "Unknown"
            };

            var changedFields = entry.State switch
            {
                EntityState.Modified => entry.Properties
                    .Where(property => property.IsModified)
                    .Select(property => property.Metadata.Name)
                    .Where(name => !ExcludedAuditPropertyNames.Contains(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                _ => []
            };

            auditLogs.Add(new AuditLog(
                Guid.NewGuid(),
                entry.Metadata.ClrType.Name,
                entityId,
                action,
                actor.Identifier,
                actor.DisplayName,
                actor.ActorType,
                JsonSerializer.Serialize(changedFields, JsonOptions),
                now));
        }

        if (auditLogs.Count > 0)
        {
            AuditLogs.AddRange(auditLogs);
        }
    }

    private static string? TryReadEntityId(EntityEntry entry)
    {
        if (entry.Properties.FirstOrDefault(property => property.Metadata.Name == "Id")?.CurrentValue is Guid guidValue)
        {
            return guidValue.ToString();
        }

        if (entry.Properties.FirstOrDefault(property => property.Metadata.Name == "Id")?.CurrentValue is string stringValue)
        {
            return stringValue;
        }

        return null;
    }
}
