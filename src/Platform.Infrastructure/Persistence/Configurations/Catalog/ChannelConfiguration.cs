using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Catalog.Channels;

namespace Platform.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channel", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.HostName).HasMaxLength(256);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property<Guid?>("TenantId");
        builder.Property<string?>("ExternalId").HasMaxLength(128);
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);

        builder.HasIndex("TenantId", nameof(Channel.Code)).IsUnique();

        builder.HasMany(x => x.MarketAssignments)
            .WithOne()
            .HasForeignKey("ChannelId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.MarketAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !EF.Property<bool>(x, "IsDeleted"));
    }
}
