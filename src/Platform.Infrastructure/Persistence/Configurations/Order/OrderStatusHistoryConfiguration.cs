using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Orders;

namespace Platform.Infrastructure.Persistence.Configurations.Order;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistory", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.FromStatus).HasMaxLength(32);
        builder.Property(x => x.ToStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(512);
        builder.Property(x => x.ChangedAtUtc).IsRequired();

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ChangedAtUtc);
    }
}
