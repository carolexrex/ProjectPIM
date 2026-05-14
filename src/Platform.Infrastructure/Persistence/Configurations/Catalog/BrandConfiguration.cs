using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Brands;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brand", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.WebsiteUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.LogoMediaAssetId);

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .HasMaxLength(64)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.Property<Guid?>("TenantId");
        builder.Property<string?>("ExternalId").HasMaxLength(128);
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);

        builder.HasIndex("TenantId", nameof(Brand.Code))
            .IsUnique();

        builder.HasOne<Platform.Domain.Catalog.Media.MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.LogoMediaAssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey("BrandId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !EF.Property<bool>(x, "IsDeleted"));
    }
}
