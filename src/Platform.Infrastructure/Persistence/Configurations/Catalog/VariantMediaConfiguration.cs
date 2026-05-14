using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Media;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class VariantMediaConfiguration : IEntityTypeConfiguration<VariantMedia>
{
    public void Configure(EntityTypeBuilder<VariantMedia> builder)
    {
        builder.ToTable("VariantMedia", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.MediaAssetId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property<Guid>("VariantId");

        builder.HasIndex("VariantId", nameof(VariantMedia.MediaAssetId), nameof(VariantMedia.Type))
            .IsUnique();

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
