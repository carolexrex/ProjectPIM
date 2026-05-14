using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Brands;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class BrandTranslationConfiguration : IEntityTypeConfiguration<BrandTranslation>
{
    public void Configure(EntityTypeBuilder<BrandTranslation> builder)
    {
        builder.ToTable("BrandTranslation", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CultureCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Description);

        builder.Property<Guid>("BrandId");

        builder.HasIndex("BrandId", nameof(BrandTranslation.CultureCode))
            .IsUnique();
    }
}
