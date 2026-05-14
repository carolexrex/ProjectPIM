using Platform.Application.Security.AdminUsers;
using Platform.Application.Security.AdminUsers.Queries;
using Platform.Domain.Security;
using Platform.Infrastructure.Catalog;

namespace Platform.Infrastructure.Security.AdminUsers;

public sealed class InMemoryAdminUserRepository : IAdminUserRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryAdminUserRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<AdminUserListResult> ListAsync(ListAdminUsersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = _store.AdminUsers.Values
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Username.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || x.DisplayName.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase));

        filtered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => filtered.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Username),
            "updatedatutc" => filtered.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Username),
            "-displayname" => filtered.OrderByDescending(x => x.DisplayName).ThenBy(x => x.Username),
            "displayname" => filtered.OrderBy(x => x.DisplayName).ThenBy(x => x.Username),
            _ => filtered.OrderBy(x => x.Username)
        };

        var materialized = filtered.ToList();
        return Task.FromResult(new AdminUserListResult(materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(), materialized.Count));
    }

    public Task<AdminUser?> GetByIdAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        _store.AdminUsers.TryGetValue(adminUserId, out var adminUser);
        return Task.FromResult(adminUser);
    }

    public Task<AdminUser?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken)
    {
        var adminUser = _store.AdminUsers.Values.FirstOrDefault(x => x.NormalizedUsername == normalizedUsername);
        return Task.FromResult(adminUser);
    }

    public Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken)
    {
        _store.AdminUsers[adminUser.Id] = adminUser;
        return Task.CompletedTask;
    }
}
