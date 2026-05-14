using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CartEntity = Platform.Domain.Cart.Cart;

namespace Platform.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartConfiguration : IEntityTypeConfiguration<CartEntity>
{
    public void Configure(EntityTypeBuilder<CartEntity> builder)
    {
        builder.ToTable("Cart", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.CultureCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VatTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property<Guid?>("TenantId");
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.MarketId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Lines).WithOne().HasForeignKey("CartId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Addresses).WithOne().HasForeignKey("CartId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !EF.Property<bool>(x, "IsDeleted"));
    }
}
