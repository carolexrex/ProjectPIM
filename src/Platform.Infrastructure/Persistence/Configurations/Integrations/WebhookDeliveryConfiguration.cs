using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Persistence.Configurations.Integrations;

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDelivery", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.LastAttemptAtUtc);
        builder.Property(x => x.NextAttemptAtUtc);
        builder.Property(x => x.ResponseCode);
        builder.Property(x => x.ResponseBody).HasMaxLength(4096);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();

        builder.HasIndex(x => new { x.WebhookSubscriptionId, x.Status });
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.NextAttemptAtUtc);

        builder.HasOne<WebhookSubscription>()
            .WithMany()
            .HasForeignKey(x => x.WebhookSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
