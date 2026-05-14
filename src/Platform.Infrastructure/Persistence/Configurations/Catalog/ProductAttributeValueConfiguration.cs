using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Attributes;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("ProductAttributeValue", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductAttributeId)
            .IsRequired();

        builder.Property(x => x.AttributeOptionId);

        builder.Property(x => x.ValueText)
            .HasMaxLength(256);

        builder.Property<Guid>("ProductId");

        builder.HasIndex("ProductId", nameof(ProductAttributeValue.ProductAttributeId))
            .IsUnique();

        builder.HasOne<ProductAttribute>()
            .WithMany()
            .HasForeignKey(x => x.ProductAttributeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
