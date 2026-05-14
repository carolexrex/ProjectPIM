using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Variants;

public sealed class UpsertVariantMediaRequest
{
    [NotEmptyGuid]
    public Guid MediaAssetId { get; init; }

    [Required]
    [StringLength(32)]
    public string Type { get; init; } = "Image";

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    public bool IsPrimary { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class RemoveVariantMediaRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
