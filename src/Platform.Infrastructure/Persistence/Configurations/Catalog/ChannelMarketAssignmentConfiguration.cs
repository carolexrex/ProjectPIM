using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Channels;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ChannelMarketAssignmentConfiguration : IEntityTypeConfiguration<ChannelMarketAssignment>
{
    public void Configure(EntityTypeBuilder<ChannelMarketAssignment> builder)
    {
        builder.ToTable("ChannelMarketAssignment", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property<Guid>("ChannelId");
        builder.Property(x => x.MarketId).IsRequired();

        builder.HasIndex("ChannelId", nameof(ChannelMarketAssignment.MarketId)).IsUnique();

        builder.HasOne<Market>()
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
