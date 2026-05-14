using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class CustomerAddressCreateViewModel
{
    public Guid CustomerId { get; set; }

    [Required]
    public string Type { get; set; } = "Shipping";

    [StringLength(128)]
    public string? Attention { get; set; }

    [Required]
    [StringLength(128)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(128)]
    public string? CompanyName { get; set; }

    [Required]
    [StringLength(256)]
    public string Line1 { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Line2 { get; set; }

    [Required]
    [StringLength(32)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string City { get; set; } = string.Empty;

    [StringLength(128)]
    public string? Region { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    public bool IsDefault { get; set; }
    public IReadOnlyList<string> TypeOptions { get; set; } = [];
}
