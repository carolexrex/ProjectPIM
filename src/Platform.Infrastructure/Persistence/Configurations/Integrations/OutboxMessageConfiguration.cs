using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Persistence.Configurations.Integrations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <summary>
    /// Name of the partial unique index that keeps at most one unpublished message per dedup key.
    /// <see cref="Platform.Infrastructure.Persistence.EfUnitOfWork"/> matches on it to translate a
    /// PostgreSQL unique violation into a domain-level duplicate signal.
    /// </summary>
    public const string ReservedDedupKeyIndexName = "IX_OutboxMessage_DedupKey_Unpublished";

    /// <summary>
    /// PostgreSQL filter for <see cref="ReservedDedupKeyIndexName"/>. It mirrors
    /// <see cref="OutboxMessage.ReservesDedupKey"/>: abandoned messages still hold their key.
    /// </summary>
    public const string ReservedDedupKeyIndexFilter = "\"DedupKey\" IS NOT NULL AND \"PublishedAtUtc\" IS NULL";

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
        builder.Property(x => x.DedupKey).HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();

        builder.HasIndex(x => x.PublishedAtUtc);
        builder.HasIndex(x => x.NextProcessingAttemptAtUtc);
        builder.HasIndex(x => x.ProcessingAbandonedAtUtc);
        builder.HasIndex(x => new { x.AggregateType, x.AggregateId });

        // PostgreSQL partial unique index: a dedup key is reserved for as long as the message is
        // unpublished — abandoned included — and is released only by publishing. The filter is
        // provider-specific raw SQL and assumes PostgreSQL quoting.
        builder.HasIndex(x => x.DedupKey)
            .HasDatabaseName(ReservedDedupKeyIndexName)
            .IsUnique()
            .HasFilter(ReservedDedupKeyIndexFilter);
    }
}
