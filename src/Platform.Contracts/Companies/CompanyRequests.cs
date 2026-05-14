using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Companies;

public class CreateCompanyRequest
{
    [StringLength(128)]
    public string? ExternalId { get; init; }

    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Name { get; init; } = string.Empty;

    [StringLength(256)]
    public string? LegalName { get; init; }

    [StringLength(64)]
    public string? OrganizationNumber { get; init; }

    [StringLength(64)]
    public string? VatNumber { get; init; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    [StringLength(64)]
    public string? Phone { get; init; }

    [NotEmptyGuid]
    public Guid? DefaultMarketId { get; init; }

    [StringLength(3, MinimumLength = 3)]
    public string? DefaultCurrency { get; init; }

    [Required]
    [StringLength(32)]
    public string Status { get; init; } = "Active";
}

public sealed class UpdateCompanyRequest : CreateCompanyRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AddCompanyAddressRequest
{
    [Required]
    [StringLength(32)]
    public string Type { get; init; } = "Billing";

    [StringLength(128)]
    public string? Attention { get; init; }

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

    public bool IsDefault { get; init; }
}

public sealed class CreateCompanyMembershipRequest
{
    [NotEmptyGuid]
    public Guid CustomerId { get; init; }

    [Required]
    [StringLength(64)]
    public string Role { get; init; } = "Buyer";

    [Required]
    [StringLength(32)]
    public string Status { get; init; } = "Active";

    public bool IsDefaultCompany { get; init; }
    public bool CanPlaceOrders { get; init; }
    public bool CanApproveOrders { get; init; }
    public bool CanManageUsers { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }
}

public sealed class UpdateCompanyMembershipRequest
{
    [Required]
    [StringLength(64)]
    public string Role { get; init; } = "Buyer";

    [Required]
    [StringLength(32)]
    public string Status { get; init; } = "Active";

    public bool IsDefaultCompany { get; init; }
    public bool CanPlaceOrders { get; init; }
    public bool CanApproveOrders { get; init; }
    public bool CanManageUsers { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
