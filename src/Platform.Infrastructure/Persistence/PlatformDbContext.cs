using Microsoft.EntityFrameworkCore;
using Platform.Application.Abstractions.Persistence;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Persistence;

public sealed class PlatformDbContext : DbContext, IUnitOfWork
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<ProductStatusDefinition> ProductStatusDefinitions => Set<ProductStatusDefinition>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<VariantAttributeValue> VariantAttributeValues => Set<VariantAttributeValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
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
}
