namespace Platform.Domain.Customers;

public sealed class CustomerAddress
{
    private CustomerAddress()
    {
        Id = Guid.Empty;
        CustomerId = Guid.Empty;
        Type = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Line1 = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        CountryCode = string.Empty;
    }

    public CustomerAddress(
        Guid id,
        Guid customerId,
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
        Id = id;
        CustomerId = customerId;
        Type = NormalizeRequired(type);
        Attention = NormalizeOptional(attention);
        FirstName = NormalizeRequired(firstName);
        LastName = NormalizeRequired(lastName);
        CompanyName = NormalizeOptional(companyName);
        Line1 = NormalizeRequired(line1);
        Line2 = NormalizeOptional(line2);
        PostalCode = NormalizeRequired(postalCode);
        City = NormalizeRequired(city);
        Region = NormalizeOptional(region);
        CountryCode = NormalizeRequired(countryCode).ToUpperInvariant();
        Phone = NormalizeOptional(phone);
        Email = NormalizeEmail(email);
        IsDefault = isDefault;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Type { get; private set; }
    public string? Attention { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? CompanyName { get; private set; }
    public string Line1 { get; private set; }
    public string? Line2 { get; private set; }
    public string PostalCode { get; private set; }
    public string City { get; private set; }
    public string? Region { get; private set; }
    public string CountryCode { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsDefault { get; private set; }

    public bool HasType(string type)
    {
        return string.Equals(Type, type, StringComparison.OrdinalIgnoreCase);
    }

    public void ClearDefault()
    {
        IsDefault = false;
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
}
