using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductRelationConfiguration : IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(EntityTypeBuilder<ProductRelation> builder)
    {
        builder.ToTable("ProductRelation", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TargetProductId)
            .IsRequired();

        builder.Property(x => x.RelationType)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 4);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property<Guid>("ProductId");

        builder.HasIndex("ProductId", nameof(ProductRelation.TargetProductId), nameof(ProductRelation.RelationType))
            .IsUnique();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.TargetProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
