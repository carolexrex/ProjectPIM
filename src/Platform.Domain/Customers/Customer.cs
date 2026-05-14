using Platform.Domain.Common;

namespace Platform.Domain.Customers;

public sealed class Customer
{
    private readonly List<CustomerAddress> _addresses = [];

    private Customer()
    {
        Id = Guid.Empty;
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public Customer(
        Guid id,
        string? externalId,
        string? userId,
        string email,
        string firstName,
        string lastName,
        string? phone,
        string? preferredCulture,
        Guid? defaultMarketId,
        string status,
        bool isGuest,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        ExternalId = NormalizeOptional(externalId);
        UserId = NormalizeOptional(userId);
        SetIdentity(email, firstName, lastName, phone, preferredCulture, defaultMarketId, status, isGuest);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public string? ExternalId { get; private set; }
    public string? UserId { get; private set; }
    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Phone { get; private set; }
    public string? PreferredCulture { get; private set; }
    public Guid? DefaultMarketId { get; private set; }
    public string Status { get; private set; }
    public bool IsGuest { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses;

    public void Update(
        string? externalId,
        string? userId,
        string email,
        string firstName,
        string lastName,
        string? phone,
        string? preferredCulture,
        Guid? defaultMarketId,
        string status,
        bool isGuest,
        string rowVersion)
    {
        EnsureRowVersion(rowVersion);
        ExternalId = NormalizeOptional(externalId);
        UserId = NormalizeOptional(userId);
        SetIdentity(email, firstName, lastName, phone, preferredCulture, defaultMarketId, status, isGuest);
        Touch();
    }

    public CustomerAddress AddAddress(
        string type,
        string? attention,
        string firstName,
        string lastName,
        string? companyName,
        string line1,
        string? line2,
        string postalCode,
        string city,
        string? region,
        string countryCode,
        string? phone,
        string? email,
        bool isDefault)
    {
        if (isDefault)
        {
            foreach (var existing in _addresses.Where(x => x.HasType(type)))
            {
                existing.ClearDefault();
            }
        }

        var address = new CustomerAddress(
            Guid.NewGuid(),
            Id,
            type,
            attention,
            firstName,
            lastName,
            companyName,
            line1,
            line2,
            postalCode,
            city,
            region,
            countryCode,
            phone,
            email,
            isDefault);
        _addresses.Add(address);
        Touch();
        return address;
    }

    private void SetIdentity(
        string email,
        string firstName,
        string lastName,
        string? phone,
        string? preferredCulture,
        Guid? defaultMarketId,
        string status,
        bool isGuest)
    {
        Email = NormalizeRequired(email).ToLowerInvariant();
        NormalizedEmail = Email.ToUpperInvariant();
        FirstName = NormalizeRequired(firstName);
        LastName = NormalizeRequired(lastName);
        Phone = NormalizeOptional(phone);
        PreferredCulture = NormalizeOptional(preferredCulture);
        DefaultMarketId = defaultMarketId;
        Status = NormalizeRequired(status);
        IsGuest = isGuest;
    }

    private void EnsureRowVersion(string rowVersion)
    {
        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The customer has changed since it was loaded.");
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
}
