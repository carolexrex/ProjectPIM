using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Cart;

namespace Platform.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartAddressConfiguration : IEntityTypeConfiguration<CartAddress>
{
    public void Configure(EntityTypeBuilder<CartAddress> builder)
    {
        builder.ToTable("CartAddress", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CartId).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(128);
        builder.Property(x => x.Line1).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Line2).HasMaxLength(256);
        builder.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.City).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Region).HasMaxLength(128);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(64);

        builder.HasIndex(x => x.CartId);
        builder.HasIndex(x => new { x.CartId, x.Type });
    }
}
