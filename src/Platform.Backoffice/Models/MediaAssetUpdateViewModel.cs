using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class MediaAssetUpdateViewModel
{
    public Guid Id { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string ContentType { get; set; } = string.Empty;

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

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
