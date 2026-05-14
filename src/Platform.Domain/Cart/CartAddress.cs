namespace Platform.Domain.Cart;

public sealed class CartAddress
{
    private CartAddress()
    {
        Id = Guid.Empty;
        CartId = Guid.Empty;
        Type = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Line1 = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        CountryCode = string.Empty;
    }

    public CartAddress(
        Guid id,
        Guid cartId,
        string type,
        string firstName,
        string lastName,
        string? companyName,
        string line1,
        string? line2,
        string postalCode,
        string city,
        string? region,
        string countryCode,
        string? email,
        string? phone)
    {
        Id = id;
        CartId = cartId;
        Type = NormalizeRequired(type);
        FirstName = NormalizeRequired(firstName);
        LastName = NormalizeRequired(lastName);
        CompanyName = NormalizeOptional(companyName);
        Line1 = NormalizeRequired(line1);
        Line2 = NormalizeOptional(line2);
        PostalCode = NormalizeRequired(postalCode);
        City = NormalizeRequired(city);
        Region = NormalizeOptional(region);
        CountryCode = NormalizeRequired(countryCode).ToUpperInvariant();
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
    }

    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public string Type { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? CompanyName { get; private set; }
    public string Line1 { get; private set; }
    public string? Line2 { get; private set; }
    public string PostalCode { get; private set; }
    public string City { get; private set; }
    public string? Region { get; private set; }
    public string CountryCode { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
