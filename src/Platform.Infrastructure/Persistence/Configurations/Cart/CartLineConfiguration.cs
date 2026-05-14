using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Cart;

namespace Platform.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartLineConfiguration : IEntityTypeConfiguration<CartLine>
{
    public void Configure(EntityTypeBuilder<CartLine> builder)
    {
        builder.ToTable("CartLine", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CartId).IsRequired();
        builder.Property(x => x.VariantId).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.VariantDescription).HasMaxLength(256);
        builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VatRate).HasPrecision(9, 4).IsRequired();
        builder.Property(x => x.LineTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(512);

        builder.HasIndex(x => x.CartId);
        builder.HasIndex(x => x.VariantId);
    }
}
