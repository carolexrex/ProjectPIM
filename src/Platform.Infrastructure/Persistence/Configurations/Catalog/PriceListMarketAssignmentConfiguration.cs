using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Pricing;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class PriceListMarketAssignmentConfiguration : IEntityTypeConfiguration<PriceListMarketAssignment>
{
    public void Configure(EntityTypeBuilder<PriceListMarketAssignment> builder)
    {
        builder.ToTable("PriceListMarketAssignment", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.IsBasePriceList).IsRequired();

        builder.HasIndex("PriceListId", nameof(PriceListMarketAssignment.MarketId))
            .IsUnique();

        builder.HasOne<Platform.Domain.Catalog.Markets.Market>()
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
