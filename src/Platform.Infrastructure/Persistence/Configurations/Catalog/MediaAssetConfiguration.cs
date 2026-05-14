using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Media;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAsset", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.StorageProvider)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.PublicUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(256);

        builder.Property(x => x.AltText)
            .HasMaxLength(256);

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

        builder.HasIndex("TenantId", nameof(MediaAsset.StorageKey))
            .IsUnique();
    }
}
