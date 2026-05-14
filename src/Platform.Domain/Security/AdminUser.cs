using Platform.Domain.Common;

namespace Platform.Domain.Security;

public sealed class AdminUser
{
    private readonly List<AdminUserRoleAssignment> _roles = [];

    private AdminUser()
    {
        Id = Guid.Empty;
        Username = string.Empty;
        NormalizedUsername = string.Empty;
        PasswordHash = string.Empty;
        DisplayName = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public AdminUser(
        Guid id,
        string username,
        string passwordHash,
        string displayName,
        string status,
        IEnumerable<string> roles,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        SetIdentity(username, passwordHash, displayName, status);
        SetRoles(roles);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string NormalizedUsername { get; private set; }
    public string PasswordHash { get; private set; }
    public string DisplayName { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<AdminUserRoleAssignment> Roles => _roles;

    public void Update(string displayName, string status, IEnumerable<string> roles, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        DisplayName = NormalizeRequired(displayName);
        Status = NormalizeRequired(status);
        SetRoles(roles);
        Touch();
    }

    public void SetPasswordHash(string passwordHash, string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        PasswordHash = NormalizeRequired(passwordHash);
        Touch();
    }

    public bool IsActive()
    {
        return string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
    }

    private void SetIdentity(string username, string passwordHash, string displayName, string status)
    {
        Username = NormalizeRequired(username);
        NormalizedUsername = Username.ToUpperInvariant();
        PasswordHash = NormalizeRequired(passwordHash);
        DisplayName = NormalizeRequired(displayName);
        Status = NormalizeRequired(status);
    }

    private void SetRoles(IEnumerable<string> roles)
    {
        _roles.Clear();
        foreach (var role in roles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            _roles.Add(new AdminUserRoleAssignment(Id, role));
        }
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The admin user has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = NewRowVersion();
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
