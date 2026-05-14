using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Pricing;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class PriceListEntryConfiguration : IEntityTypeConfiguration<PriceListEntry>
{
    public void Configure(EntityTypeBuilder<PriceListEntry> builder)
    {
        builder.ToTable("PriceListEntry", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TargetType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.MinQuantity).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.CompareAtAmount).HasColumnType("numeric(18,4)");

        builder.HasIndex("PriceListId", nameof(PriceListEntry.TargetType), nameof(PriceListEntry.TargetId), nameof(PriceListEntry.MinQuantity))
            .IsUnique();
    }
}
