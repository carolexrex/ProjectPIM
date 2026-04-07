using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<VariantAttributeValue> builder)
    {
        builder.ToTable("VariantAttributeValue", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductAttributeId)
            .IsRequired();

        builder.Property(x => x.AttributeOptionId);

        builder.Property(x => x.ValueText)
            .HasMaxLength(256);

        builder.Property<Guid>("VariantId");
    }
}
