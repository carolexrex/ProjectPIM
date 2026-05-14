using Platform.Domain.Common;

namespace Platform.Domain.Companies;

public sealed class Company
{
    private readonly List<CompanyAddress> _addresses = [];
    private readonly List<CompanyMembership> _memberships = [];

    private Company()
    {
        Id = Guid.Empty;
        Code = string.Empty;
        Name = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public Company(
        Guid id,
        string? externalId,
        string code,
        string name,
        string? legalName,
        string? organizationNumber,
        string? vatNumber,
        string? email,
        string? phone,
        Guid? defaultMarketId,
        string? defaultCurrency,
        string status,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        ExternalId = NormalizeOptional(externalId);
        SetIdentity(code, name, legalName, organizationNumber, vatNumber, email, phone, defaultMarketId, defaultCurrency, status);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public string? ExternalId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? LegalName { get; private set; }
    public string? OrganizationNumber { get; private set; }
    public string? VatNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public Guid? DefaultMarketId { get; private set; }
    public string? DefaultCurrency { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<CompanyAddress> Addresses => _addresses;
    public IReadOnlyCollection<CompanyMembership> Memberships => _memberships;

    public void Update(
        string? externalId,
        string code,
        string name,
        string? legalName,
        string? organizationNumber,
        string? vatNumber,
        string? email,
        string? phone,
        Guid? defaultMarketId,
        string? defaultCurrency,
        string status,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        ExternalId = NormalizeOptional(externalId);
        SetIdentity(code, name, legalName, organizationNumber, vatNumber, email, phone, defaultMarketId, defaultCurrency, status);
        Touch();
    }

    public CompanyAddress AddAddress(
        string type,
        string? attention,
        string line1,
        string? line2,
        string postalCode,
        string city,
        string? region,
        string countryCode,
        string? email,
        string? phone,
        bool isDefault)
    {
        if (isDefault)
        {
            foreach (var existing in _addresses.Where(x => x.HasType(type)))
            {
                existing.ClearDefault();
            }
        }

        var address = new CompanyAddress(
            Guid.NewGuid(),
            Id,
            type,
            attention,
            line1,
            line2,
            postalCode,
            city,
            region,
            countryCode,
            email,
            phone,
            isDefault);
        _addresses.Add(address);
        Touch();
        return address;
    }

    public CompanyMembership AddMembership(
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
        if (_memberships.Any(x => x.CustomerId == customerId))
        {
            throw new InvalidOperationException("The customer is already a member of this company.");
        }

        var membership = new CompanyMembership(
            Guid.NewGuid(),
            Id,
            customerId,
            role,
            status,
            isDefaultCompany,
            canPlaceOrders,
            canApproveOrders,
            canManageUsers,
            validFromUtc,
            validToUtc);
        _memberships.Add(membership);
        Touch();
        return membership;
    }

    public CompanyMembership? GetMembership(Guid membershipId)
    {
        return _memberships.FirstOrDefault(x => x.Id == membershipId);
    }

    private void SetIdentity(
        string code,
        string name,
        string? legalName,
        string? organizationNumber,
        string? vatNumber,
        string? email,
        string? phone,
        Guid? defaultMarketId,
        string? defaultCurrency,
        string status)
    {
        Code = NormalizeRequired(code).ToUpperInvariant();
        Name = NormalizeRequired(name);
        LegalName = NormalizeOptional(legalName);
        OrganizationNumber = NormalizeOptional(organizationNumber);
        VatNumber = NormalizeOptional(vatNumber);
        Email = NormalizeEmail(email);
        Phone = NormalizeOptional(phone);
        DefaultMarketId = defaultMarketId;
        DefaultCurrency = NormalizeCurrency(defaultCurrency);
        Status = NormalizeRequired(status);
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The company has changed since it was loaded.");
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeCurrency(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}
