using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Products;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class MarketProductAssignmentConfiguration : IEntityTypeConfiguration<MarketProductAssignment>
{
    public void Configure(EntityTypeBuilder<MarketProductAssignment> builder)
    {
        builder.ToTable("MarketProductAssignment", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property<Guid>("MarketId");
        builder.Property(x => x.ProductId).IsRequired();

        builder.HasIndex("MarketId", nameof(MarketProductAssignment.ProductId)).IsUnique();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
