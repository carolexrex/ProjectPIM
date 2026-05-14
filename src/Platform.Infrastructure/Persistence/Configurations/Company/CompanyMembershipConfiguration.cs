using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Companies;

namespace Platform.Infrastructure.Persistence.Configurations.Company;

public sealed class CompanyMembershipConfiguration : IEntityTypeConfiguration<CompanyMembership>
{
    public void Configure(EntityTypeBuilder<CompanyMembership> builder)
    {
        builder.ToTable("CompanyMembership", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsDefaultCompany).IsRequired();
        builder.Property(x => x.CanPlaceOrders).IsRequired();
        builder.Property(x => x.CanApproveOrders).IsRequired();
        builder.Property(x => x.CanManageUsers).IsRequired();
        builder.Property(x => x.ValidFromUtc);
        builder.Property(x => x.ValidToUtc);
        builder.Property(x => x.RowVersion).HasMaxLength(64).IsConcurrencyToken().IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.CustomerId }).IsUnique();
        builder.HasIndex(x => x.CustomerId);
    }
}
