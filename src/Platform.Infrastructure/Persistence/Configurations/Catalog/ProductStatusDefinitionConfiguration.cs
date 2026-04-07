using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductStatusDefinitionConfiguration : IEntityTypeConfiguration<ProductStatusDefinition>
{
    public void Configure(EntityTypeBuilder<ProductStatusDefinition> builder)
    {
        builder.ToTable("ProductStatusDefinition", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.IsBuyable)
            .IsRequired();

        builder.Property<Guid?>("TenantId");
        builder.Property<string>("EntityType").HasMaxLength(32);
        builder.Property<bool>("IsDefault").HasDefaultValue(false);
        builder.Property<bool>("IsVisibleInBackoffice").HasDefaultValue(true);
        builder.Property<bool>("IsVisibleInStorefront").HasDefaultValue(true);
        builder.Property<bool>("IsSearchable").HasDefaultValue(true);
        builder.Property<int>("SortOrder").HasDefaultValue(0);
        builder.Property<string>("Status").HasMaxLength(32);
        builder.Property<DateTime>("CreatedAtUtc");
        builder.Property<DateTime>("UpdatedAtUtc");
        builder.Property<byte[]>("RowVersion")
            .HasColumnType("rowversion")
            .IsRowVersion();

        builder.HasIndex("TenantId", "EntityType", nameof(ProductStatusDefinition.Code))
            .IsUnique();
    }
}
