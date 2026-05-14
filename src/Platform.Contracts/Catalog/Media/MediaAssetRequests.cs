using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Catalog.Media;

public sealed class CreateMediaAssetRequest
{
    [Required]
    [StringLength(64)]
    public string StorageProvider { get; init; } = "External";

    [Required]
    [StringLength(512)]
    public string StorageKey { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string ContentType { get; init; } = "image/jpeg";

    [Range(0, long.MaxValue)]
    public long FileSize { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }

    [Required]
    [StringLength(2048)]
    public string PublicUrl { get; init; } = string.Empty;

    [StringLength(256)]
    public string? Title { get; init; }

    [StringLength(256)]
    public string? AltText { get; init; }
}

public sealed class UpdateMediaAssetRequest
{
    [Required]
    [StringLength(256)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string ContentType { get; init; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long FileSize { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }

    [Required]
    [StringLength(2048)]
    public string PublicUrl { get; init; } = string.Empty;

    [StringLength(256)]
    public string? Title { get; init; }

    [StringLength(256)]
    public string? AltText { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class ArchiveMediaAssetRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
