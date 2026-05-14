using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
{
    public void Configure(EntityTypeBuilder<ProductTranslation> builder)
    {
        builder.ToTable("ProductTranslation", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CultureCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ShortDescription)
            .HasMaxLength(1024);

        builder.Property(x => x.SeoTitle)
            .HasMaxLength(256);

        builder.Property(x => x.SeoDescription)
            .HasMaxLength(512);

        builder.Property<Guid>("ProductId");
        builder.Property<DateTime>("CreatedAtUtc");
        builder.Property<DateTime>("UpdatedAtUtc");

        builder.HasIndex("ProductId", nameof(ProductTranslation.CultureCode))
            .IsUnique();
    }
}
