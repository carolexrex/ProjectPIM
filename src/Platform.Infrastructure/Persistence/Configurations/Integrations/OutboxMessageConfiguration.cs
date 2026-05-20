using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Persistence.Configurations.Integrations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessage", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AggregateType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.PublishedAtUtc);
        builder.Property(x => x.ProcessingAttemptCount).IsRequired();
        builder.Property(x => x.LastProcessingError).HasMaxLength(2048);
        builder.Property(x => x.NextProcessingAttemptAtUtc);
        builder.Property(x => x.ProcessingAbandonedAtUtc);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();

        builder.HasIndex(x => x.PublishedAtUtc);
        builder.HasIndex(x => x.NextProcessingAttemptAtUtc);
        builder.HasIndex(x => x.ProcessingAbandonedAtUtc);
        builder.HasIndex(x => new { x.AggregateType, x.AggregateId });
    }
}
