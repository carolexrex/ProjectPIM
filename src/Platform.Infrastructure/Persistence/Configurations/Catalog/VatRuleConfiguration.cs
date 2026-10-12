using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Tax;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class VatRuleConfiguration : IEntityTypeConfiguration<VatRule>
{
    /// <summary>
    /// Name of the unique index that keeps one VAT rule per tenant, market, and tax category.
    /// <see cref="Platform.Infrastructure.Persistence.EfUnitOfWork"/> matches on it to translate a
    /// PostgreSQL unique violation from concurrent creates into a conflict.
    /// </summary>
    public const string MarketTaxCategoryIndexName = "UX_VatRule_TenantId_MarketId_TaxCategoryCode";

    public void Configure(EntityTypeBuilder<VatRule> builder)
    {
        // PostgreSQL check constraint (assumes PostgreSQL identifier quoting): the domain rejects
        // out-of-range rates, this keeps direct SQL writes honest too.
        builder.ToTable("VatRule", "public", table =>
            table.HasCheckConstraint("CK_VatRule_Rate", "\"Rate\" >= 0 AND \"Rate\" < 1"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MarketId).IsRequired();
        builder.Property(x => x.TaxCategoryCode).HasMaxLength(VatRule.MaxTaxCategoryCodeLength).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(9, VatRule.RateDecimalPlaces).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(VatRule.MaxDescriptionLength);
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property<Guid?>("TenantId");

        // NULLS NOT DISTINCT (PostgreSQL 15+) so the single-tenant NULL TenantId still enforces uniqueness.
        builder.HasIndex("TenantId", nameof(VatRule.MarketId), nameof(VatRule.TaxCategoryCode))
            .HasDatabaseName(MarketTaxCategoryIndexName)
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasOne<Market>()
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
