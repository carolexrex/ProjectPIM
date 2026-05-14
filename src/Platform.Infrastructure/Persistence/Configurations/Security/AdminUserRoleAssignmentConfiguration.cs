using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Security;

namespace Platform.Infrastructure.Persistence.Configurations.Security;

public sealed class AdminUserRoleAssignmentConfiguration : IEntityTypeConfiguration<AdminUserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<AdminUserRoleAssignment> builder)
    {
        builder.ToTable("AdminUserRole", "public");

        builder.HasKey(x => new { x.AdminUserId, x.Role });
        builder.Property(x => x.AdminUserId).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(64).IsRequired();
    }
}
