using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Markets;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("Market", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DefaultCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.DefaultCulture).HasMaxLength(16).IsRequired();
        builder.Property(x => x.VatMode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property<Guid?>("TenantId");
        builder.Property<string?>("ExternalId").HasMaxLength(128);
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);

        builder.HasIndex("TenantId", nameof(Market.Code)).IsUnique();

        builder.HasMany(x => x.Currencies).WithOne().HasForeignKey("MarketId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Cultures).WithOne().HasForeignKey("MarketId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ProductAssignments).WithOne().HasForeignKey("MarketId").OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Currencies).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Cultures).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ProductAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !EF.Property<bool>(x, "IsDeleted"));
    }
}
