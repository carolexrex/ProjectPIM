using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Storefront;

public sealed class CreateStorefrontCartRequest
{
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<StorefrontCartLineRequest> Lines { get; init; } = [];

    public IReadOnlyList<StorefrontCartAddressRequest> Addresses { get; init; } = [];
}

public sealed class StorefrontCartLineRequest
{
    [NotEmptyGuid]
    public Guid VariantId { get; init; }

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; init; }

    [StringLength(512)]
    public string? Comment { get; init; }
}

public sealed class StorefrontCartAddressRequest
{
    [Required]
    [StringLength(32)]
    public string Type { get; init; } = string.Empty;

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

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    [StringLength(64)]
    public string? Phone { get; init; }
}

public sealed class RepriceStorefrontCartRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class CheckoutStorefrontCartRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
