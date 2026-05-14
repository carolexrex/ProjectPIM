using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Orders;

public sealed class CreateOrderRequest
{
    [NotEmptyGuid]
    public Guid? CartId { get; init; }

    public string? CartRowVersion { get; init; }

    [NotEmptyGuid]
    public Guid? CustomerId { get; init; }

    [NotEmptyGuid]
    public Guid? CompanyId { get; init; }

    [NotEmptyGuid]
    public Guid? MarketId { get; init; }

    [StringLength(3, MinimumLength = 3)]
    public string? CurrencyCode { get; init; }

    [StringLength(16)]
    public string? CultureCode { get; init; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    public IReadOnlyList<CreateOrderLineRequest> Lines { get; init; } = [];
    public IReadOnlyList<CreateOrderAddressRequest> Addresses { get; init; } = [];
}

public sealed class CreateOrderLineRequest
{
    [NotEmptyGuid]
    public Guid VariantId { get; init; }

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; init; }

    [StringLength(512)]
    public string? Comment { get; init; }
}

public sealed class CreateOrderAddressRequest
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

public sealed class ChangeOrderStatusRequest
{
    [Required]
    [StringLength(32)]
    public string ToStatus { get; init; } = string.Empty;

    [StringLength(512)]
    public string? Comment { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AddPaymentTransactionRequest
{
    [Required]
    [StringLength(64)]
    public string Provider { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string ProviderReference { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Type { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Status { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; init; } = string.Empty;

    public DateTime RequestedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
