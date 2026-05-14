using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class MarketCurrencyConfiguration : IEntityTypeConfiguration<MarketCurrency>
{
    public void Configure(EntityTypeBuilder<MarketCurrency> builder)
    {
        builder.ToTable("MarketCurrency", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property<Guid>("MarketId");
        builder.HasIndex("MarketId", nameof(MarketCurrency.CurrencyCode)).IsUnique();
    }
}
