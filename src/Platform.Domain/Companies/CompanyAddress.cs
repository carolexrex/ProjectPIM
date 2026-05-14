namespace Platform.Domain.Companies;

public sealed class CompanyAddress
{
    private CompanyAddress()
    {
        Id = Guid.Empty;
        CompanyId = Guid.Empty;
        Type = string.Empty;
        Line1 = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        CountryCode = string.Empty;
    }

    public CompanyAddress(
        Guid id,
        Guid companyId,
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
        Id = id;
        CompanyId = companyId;
        Type = NormalizeRequired(type);
        Attention = NormalizeOptional(attention);
        Line1 = NormalizeRequired(line1);
        Line2 = NormalizeOptional(line2);
        PostalCode = NormalizeRequired(postalCode);
        City = NormalizeRequired(city);
        Region = NormalizeOptional(region);
        CountryCode = NormalizeRequired(countryCode).ToUpperInvariant();
        Email = NormalizeEmail(email);
        Phone = NormalizeOptional(phone);
        IsDefault = isDefault;
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Type { get; private set; }
    public string? Attention { get; private set; }
    public string Line1 { get; private set; }
    public string? Line2 { get; private set; }
    public string PostalCode { get; private set; }
    public string City { get; private set; }
    public string? Region { get; private set; }
    public string CountryCode { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
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
