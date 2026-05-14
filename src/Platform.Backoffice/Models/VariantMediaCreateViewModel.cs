using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class VariantMediaCreateViewModel
{
    public Guid VariantId { get; set; }

    [Required]
    public Guid MediaAssetId { get; set; }

    [Required]
    [StringLength(32)]
    public string Type { get; set; } = "Image";

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; } = true;

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<string> MediaTypeOptions { get; set; } = [];
    public IReadOnlyList<MediaAssetLookupOptionViewModel> MediaAssetOptions { get; set; } = [];
}
