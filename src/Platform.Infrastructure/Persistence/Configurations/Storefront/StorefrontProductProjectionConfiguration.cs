using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Application.Storefront;

namespace Platform.Infrastructure.Persistence.Configurations.Storefront;

public sealed class StorefrontProductProjectionConfiguration : IEntityTypeConfiguration<StorefrontProductProjection>
{
    public void Configure(EntityTypeBuilder<StorefrontProductProjection> builder)
    {
        builder.ToTable("StorefrontProductProjection", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.MarketId).IsRequired();
        builder.Property(x => x.MarketCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CultureCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.ProductNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ProductType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(2048);
        builder.Property(x => x.LongDescription);
        builder.Property(x => x.SeoTitle).HasMaxLength(512);
        builder.Property(x => x.SeoDescription).HasMaxLength(2048);
        builder.Property(x => x.BrandCode).HasMaxLength(64);
        builder.Property(x => x.BrandName).HasMaxLength(256);
        builder.Property(x => x.BrandSlug).HasMaxLength(256);
        builder.Property(x => x.BrandWebsiteUrl).HasMaxLength(2048);
        builder.Property(x => x.BrandLogoUrl).HasMaxLength(2048);
        builder.Property(x => x.CategoryCodesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CategorySlugsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CategoryNamesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CategoryFilterSlugsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CategoriesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PrimaryImageUrl).HasMaxLength(2048);
        builder.Property(x => x.AttributesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.MediaJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.BuyabilityReasonsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AvailabilityStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PriceAmount).HasPrecision(18, 2);
        builder.Property(x => x.CompareAtAmount).HasPrecision(18, 2);
        builder.Property(x => x.PriceListCode).HasMaxLength(64);
        builder.Property(x => x.VariantsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SearchText).HasMaxLength(4096).IsRequired();
        builder.Property(x => x.SortName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.SortProductNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SortPriceAmount).HasPrecision(18, 2);
        builder.Property(x => x.BrandSortName).HasMaxLength(256);
        builder.Property(x => x.SourceUpdatedAtUtc).IsRequired();
        builder.Property(x => x.ProjectedAtUtc).IsRequired();
        builder.Property(x => x.ProjectionVersion).HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();

        builder.HasIndex(x => new { x.MarketCode, x.CultureCode, x.CurrencyCode, x.ProductNumber }).IsUnique();
        builder.HasIndex(x => new { x.MarketCode, x.CultureCode, x.CurrencyCode, x.Slug }).IsUnique();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.BrandCode);
        builder.HasIndex(x => x.IsVisible);
        builder.HasIndex(x => x.IsBuyable);
        builder.HasIndex(x => x.AvailabilityStatus);
        builder.HasIndex(x => x.SortName);
        builder.HasIndex(x => x.SortPriceAmount);
    }
}
