using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Categories;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
    {
        builder.ToTable("CategoryTranslation", "public");

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

        builder.Property<Guid>("CategoryId");

        builder.HasIndex("CategoryId", nameof(CategoryTranslation.CultureCode))
            .IsUnique();
    }
}
