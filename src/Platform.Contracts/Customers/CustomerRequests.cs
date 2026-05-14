using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Customers;

public class CreateCustomerRequest
{
    [StringLength(128)]
    public string? ExternalId { get; init; }

    [StringLength(128)]
    public string? UserId { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string LastName { get; init; } = string.Empty;

    [StringLength(64)]
    public string? Phone { get; init; }

    [StringLength(16)]
    public string? PreferredCulture { get; init; }

    [NotEmptyGuid]
    public Guid? DefaultMarketId { get; init; }

    [Required]
    [StringLength(32)]
    public string Status { get; init; } = "Active";

    public bool IsGuest { get; init; }
}

public sealed class UpdateCustomerRequest : CreateCustomerRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AddCustomerAddressRequest
{
    [Required]
    [StringLength(32)]
    public string Type { get; init; } = "Shipping";

    [StringLength(128)]
    public string? Attention { get; init; }

    [Required]
    [StringLength(128)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string LastName { get; init; } = string.Empty;

    [StringLength(128)]
    public string? CompanyName { get; init; }

    [Required]
    [StringLength(256)]
    public string Line1 { get; init; } = string.Empty;

    [StringLength(256)]
    public string? Line2 { get; init; }

    [Required]
    [StringLength(32)]
    public string PostalCode { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string City { get; init; } = string.Empty;

    [StringLength(128)]
    public string? Region { get; init; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; init; } = string.Empty;

    [StringLength(64)]
    public string? Phone { get; init; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    public bool IsDefault { get; init; }
}
