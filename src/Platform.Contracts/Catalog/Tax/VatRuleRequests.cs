using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Tax;

public sealed class CreateVatRuleRequest
{
    [NotEmptyGuid]
    public Guid MarketId { get; init; }

    [Required]
    [StringLength(64)]
    public string TaxCategoryCode { get; init; } = string.Empty;

    // Nullable so an omitted rate is rejected instead of silently binding to 0%.
    [Required]
    [Range(0d, 0.9999d)]
    public decimal? Rate { get; init; }

    [StringLength(256)]
    public string? Description { get; init; }
}

public sealed class UpdateVatRuleRequest
{
    [Required]
    [Range(0d, 0.9999d)]
    public decimal? Rate { get; init; }

    [StringLength(256)]
    public string? Description { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class DeleteVatRuleRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
