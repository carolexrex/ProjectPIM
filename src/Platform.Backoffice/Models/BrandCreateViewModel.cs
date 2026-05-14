using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class BrandCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? WebsiteUrl { get; set; }

    public Guid? LogoMediaAssetId { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    public IReadOnlyList<MediaAssetLookupOptionViewModel> LogoOptions { get; set; } = [];
}
