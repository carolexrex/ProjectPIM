using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Products;

public sealed class UpsertProductMediaRequest
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

public sealed class RemoveProductMediaRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
