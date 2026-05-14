using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Attributes;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.ToTable("AttributeOption", "public");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property<Guid>("ProductAttributeId");

        builder.HasIndex("ProductAttributeId", nameof(AttributeOption.Code))
            .IsUnique();
    }
}
