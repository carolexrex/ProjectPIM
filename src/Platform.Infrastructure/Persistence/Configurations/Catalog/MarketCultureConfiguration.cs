using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class MarketCultureConfiguration : IEntityTypeConfiguration<MarketCulture>
{
    public void Configure(EntityTypeBuilder<MarketCulture> builder)
    {
        builder.ToTable("MarketCulture", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CultureCode).HasMaxLength(16).IsRequired();
        builder.Property<Guid>("MarketId");
        builder.HasIndex("MarketId", nameof(MarketCulture.CultureCode)).IsUnique();
    }
}
