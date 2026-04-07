using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private static readonly ValueConverter<string, byte[]> RowVersionConverter = new(
        value => string.IsNullOrWhiteSpace(value) ? [] : Convert.FromBase64String(value),
        value => value.Length == 0 ? string.Empty : Convert.ToBase64String(value));

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ProductNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.BrandId);

        builder.Property(x => x.TaxCategoryCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.UnitOfMeasure)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Weight)
            .HasPrecision(18, 4);

        builder.Property(x => x.Length)
            .HasPrecision(18, 4);

        builder.Property(x => x.Width)
            .HasPrecision(18, 4);

        builder.Property(x => x.Height)
            .HasPrecision(18, 4);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .HasColumnType("rowversion")
            .HasConversion(RowVersionConverter)
            .IsRowVersion();

        builder.Ignore(x => x.PrimaryImageUrl);

        builder.Property<Guid?>("TenantId");
        builder.Property<string?>("ExternalId").HasMaxLength(128);
        builder.Property<Guid>("ProductStatusDefinitionId");
        builder.Property<Guid?>("PrimaryImageMediaAssetId");
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);

        builder.HasIndex("TenantId", nameof(Product.ProductNumber))
            .IsUnique();

        builder.HasIndex("TenantId", nameof(Product.Slug))
            .IsUnique();

        builder.HasOne(x => x.ProductStatus)
            .WithMany()
            .HasForeignKey("ProductStatusDefinitionId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey("ProductId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !EF.Property<bool>(x, "IsDeleted"));
    }
}
