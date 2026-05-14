using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Inventory;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class InventoryLocationConfiguration : IEntityTypeConfiguration<InventoryLocation>
{
    public void Configure(EntityTypeBuilder<InventoryLocation> builder)
    {
        builder.ToTable("InventoryLocation", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CountryCode).HasMaxLength(2);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property<Guid?>("TenantId");
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);

        builder.HasIndex("TenantId", nameof(InventoryLocation.Code)).IsUnique();

        builder.HasMany(x => x.MarketAssignments).WithOne().HasForeignKey("InventoryLocationId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.MarketAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !EF.Property<bool>(x, "IsDeleted"));
    }
}
