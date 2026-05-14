using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Categories;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductCategoryAssignmentConfiguration : IEntityTypeConfiguration<ProductCategoryAssignment>
{
    public void Configure(EntityTypeBuilder<ProductCategoryAssignment> builder)
    {
        builder.ToTable("ProductCategory", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CategoryId)
            .IsRequired();

        builder.Property<Guid>("ProductId");

        builder.HasIndex("ProductId", nameof(ProductCategoryAssignment.CategoryId))
            .IsUnique();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
