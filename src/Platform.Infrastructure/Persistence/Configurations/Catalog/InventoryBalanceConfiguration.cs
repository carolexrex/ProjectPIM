using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalance", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OnHandQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.ReservedQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.IncomingQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Backorderable).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();

        builder.HasIndex(x => new { x.InventoryLocationId, x.VariantId }).IsUnique();

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Variant>()
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
