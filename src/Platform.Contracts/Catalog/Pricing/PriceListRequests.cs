using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Pricing;

public sealed class CreatePriceListRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; init; } = string.Empty;

    public bool VatIncluded { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }
}

public sealed class UpdatePriceListRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; init; } = string.Empty;

    public bool VatIncluded { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertPriceListMarketAssignmentRequest
{
    [NotEmptyGuid]
    public Guid MarketId { get; init; }

    [Range(0, int.MaxValue)]
    public int Priority { get; init; }

    public bool IsBasePriceList { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class RemovePriceListMarketAssignmentRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertPriceListEntryRequest
{
    public Guid? EntryId { get; init; }

    [Required]
    [StringLength(32)]
    public string TargetType { get; init; } = "Variant";

    [NotEmptyGuid]
    public Guid TargetId { get; init; }

    [Range(1, int.MaxValue)]
    public int MinQuantity { get; init; } = 1;

    public decimal Amount { get; init; }

    public decimal? CompareAtAmount { get; init; }

    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class RemovePriceListEntryRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
