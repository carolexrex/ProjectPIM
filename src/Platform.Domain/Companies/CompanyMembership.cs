using Platform.Domain.Common;

namespace Platform.Domain.Companies;

public sealed class CompanyMembership
{
    private CompanyMembership()
    {
        Id = Guid.Empty;
        CompanyId = Guid.Empty;
        CustomerId = Guid.Empty;
        Role = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public CompanyMembership(
        Guid id,
        Guid companyId,
        Guid customerId,
        string role,
        string status,
        bool isDefaultCompany,
        bool canPlaceOrders,
        bool canApproveOrders,
        bool canManageUsers,
        DateTime? validFromUtc,
        DateTime? validToUtc)
    {
        EnsureValidityWindow(validFromUtc, validToUtc);

        Id = id;
        CompanyId = companyId;
        CustomerId = customerId;
        Role = NormalizeRequired(role);
        Status = NormalizeRequired(status);
        IsDefaultCompany = isDefaultCompany;
        CanPlaceOrders = canPlaceOrders;
        CanApproveOrders = canApproveOrders;
        CanManageUsers = canManageUsers;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Role { get; private set; }
    public string Status { get; private set; }
    public bool IsDefaultCompany { get; private set; }
    public bool CanPlaceOrders { get; private set; }
    public bool CanApproveOrders { get; private set; }
    public bool CanManageUsers { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidToUtc { get; private set; }
    public string RowVersion { get; private set; }

    public void Update(
        string role,
        string status,
        bool isDefaultCompany,
        bool canPlaceOrders,
        bool canApproveOrders,
        bool canManageUsers,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        EnsureValidityWindow(validFromUtc, validToUtc);

        Role = NormalizeRequired(role);
        Status = NormalizeRequired(status);
        IsDefaultCompany = isDefaultCompany;
        CanPlaceOrders = canPlaceOrders;
        CanApproveOrders = canApproveOrders;
        CanManageUsers = canManageUsers;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        RowVersion = NewRowVersion();
    }

    public bool IsValidAt(DateTime instantUtc)
    {
        if (ValidFromUtc.HasValue && instantUtc < ValidFromUtc.Value)
        {
            return false;
        }

        if (ValidToUtc.HasValue && instantUtc > ValidToUtc.Value)
        {
            return false;
        }

        return true;
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The company membership has changed since it was loaded.");
        }
    }

    private static void EnsureValidityWindow(DateTime? validFromUtc, DateTime? validToUtc)
    {
        if (validFromUtc.HasValue && validToUtc.HasValue && validToUtc.Value < validFromUtc.Value)
        {
            throw new InvalidOperationException("Membership valid-to date must be greater than or equal to valid-from date.");
        }
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
