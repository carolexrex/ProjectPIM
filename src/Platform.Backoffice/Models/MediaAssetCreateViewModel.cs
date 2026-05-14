using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class MediaAssetCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string StorageProvider { get; set; } = "External";

    [Required]
    [StringLength(512)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string ContentType { get; set; } = "image/jpeg";

    [Range(0, long.MaxValue)]
    public long FileSize { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }

    [Required]
    [StringLength(2048)]
    public string PublicUrl { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Title { get; set; }

    [StringLength(256)]
    public string? AltText { get; set; }
}
