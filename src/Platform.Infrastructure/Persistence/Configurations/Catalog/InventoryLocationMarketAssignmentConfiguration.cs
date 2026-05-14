using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class InventoryLocationMarketAssignmentConfiguration : IEntityTypeConfiguration<InventoryLocationMarketAssignment>
{
    public void Configure(EntityTypeBuilder<InventoryLocationMarketAssignment> builder)
    {
        builder.ToTable("InventoryLocationMarketAssignment", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property<Guid>("InventoryLocationId");

        builder.HasIndex("InventoryLocationId", nameof(InventoryLocationMarketAssignment.MarketId)).IsUnique();

        builder.HasOne<Market>()
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
