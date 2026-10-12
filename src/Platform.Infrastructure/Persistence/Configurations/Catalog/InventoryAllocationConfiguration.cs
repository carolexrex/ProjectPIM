using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Inventory;
using OrderEntity = Platform.Domain.Orders.Order;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class InventoryAllocationConfiguration : IEntityTypeConfiguration<InventoryAllocation>
{
    public const string OrderBalanceIndexName = "UX_InventoryAllocation_OrderId_InventoryBalanceId";

    public void Configure(EntityTypeBuilder<InventoryAllocation> builder)
    {
        builder.ToTable("InventoryAllocation", "public", table =>
        {
            // PostgreSQL identifier quoting; the domain enforces the same rules.
            table.HasCheckConstraint("CK_InventoryAllocation_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint(
                "CK_InventoryAllocation_BackorderedQuantity",
                "\"BackorderedQuantity\" >= 0 AND \"BackorderedQuantity\" <= \"Quantity\"");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.BackorderedQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();
        builder.Ignore(x => x.IsOutstanding);

        // One allocation per order and balance: the planner merges an order's demand per balance.
        builder.HasIndex(x => new { x.OrderId, x.InventoryBalanceId })
            .IsUnique()
            .HasDatabaseName(OrderBalanceIndexName);
        builder.HasIndex(x => new { x.InventoryBalanceId, x.Status });
        builder.HasIndex(x => x.VariantId);

        builder.HasOne<OrderEntity>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InventoryBalance>()
            .WithMany()
            .HasForeignKey(x => x.InventoryBalanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InventoryLocation>()
            .WithMany()
            .HasForeignKey(x => x.InventoryLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
