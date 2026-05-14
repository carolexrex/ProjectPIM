using Microsoft.EntityFrameworkCore;
using Platform.Application.Security.AdminUsers;
using Platform.Application.Security.AdminUsers.Queries;
using Platform.Domain.Security;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Security.AdminUsers;

public sealed class EfAdminUserRepository : IAdminUserRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfAdminUserRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminUserListResult> ListAsync(ListAdminUsersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.AdminUsers
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Username.Contains(query.Search)
                || x.DisplayName.Contains(query.Search))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.Roles)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AdminUserListResult(items, total);
    }

    public async Task<AdminUser?> GetByIdAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        return await _dbContext.AdminUsers
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken);
    }

    public async Task<AdminUser?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken)
    {
        return await _dbContext.AdminUsers
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.NormalizedUsername == normalizedUsername, cancellationToken);
    }

    public async Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken)
    {
        await _dbContext.AdminUsers.AddAsync(adminUser, cancellationToken);
    }

    private static IQueryable<AdminUser> ApplySorting(IQueryable<AdminUser> adminUsers, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => adminUsers.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Username),
            "updatedatutc" => adminUsers.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Username),
            "-displayname" => adminUsers.OrderByDescending(x => x.DisplayName).ThenBy(x => x.Username),
            "displayname" => adminUsers.OrderBy(x => x.DisplayName).ThenBy(x => x.Username),
            _ => adminUsers.OrderBy(x => x.Username)
        };
    }
}
