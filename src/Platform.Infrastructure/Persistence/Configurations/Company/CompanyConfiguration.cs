using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CompanyEntity = Platform.Domain.Companies.Company;

namespace Platform.Infrastructure.Persistence.Configurations.Company;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<CompanyEntity>
{
    public void Configure(EntityTypeBuilder<CompanyEntity> builder)
    {
        builder.ToTable("Company", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ExternalId).HasMaxLength(128);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(256);
        builder.Property(x => x.OrganizationNumber).HasMaxLength(64);
        builder.Property(x => x.VatNumber).HasMaxLength(64);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(64);
        builder.Property(x => x.DefaultCurrency).HasMaxLength(3);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property<Guid?>("TenantId");
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);

        builder.HasIndex("TenantId", nameof(CompanyEntity.Code)).IsUnique();

        builder.HasMany(x => x.Addresses).WithOne().HasForeignKey("CompanyId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Memberships).WithOne().HasForeignKey("CompanyId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Memberships).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(x => !EF.Property<bool>(x, "IsDeleted"));
    }
}
