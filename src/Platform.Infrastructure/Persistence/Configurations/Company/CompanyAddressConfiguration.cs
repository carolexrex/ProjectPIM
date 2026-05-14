using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Companies;

namespace Platform.Infrastructure.Persistence.Configurations.Company;

public sealed class CompanyAddressConfiguration : IEntityTypeConfiguration<CompanyAddress>
{
    public void Configure(EntityTypeBuilder<CompanyAddress> builder)
    {
        builder.ToTable("CompanyAddress", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Attention).HasMaxLength(128);
        builder.Property(x => x.Line1).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Line2).HasMaxLength(256);
        builder.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.City).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Region).HasMaxLength(128);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(64);
        builder.Property(x => x.IsDefault).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Type });
    }
}
