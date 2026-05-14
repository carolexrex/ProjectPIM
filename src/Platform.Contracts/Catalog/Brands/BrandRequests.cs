using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Brands;

public sealed class CreateBrandRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [StringLength(1024)]
    public string? WebsiteUrl { get; init; }

    public Guid? LogoMediaAssetId { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
}

public sealed class UpdateBrandRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [StringLength(1024)]
    public string? WebsiteUrl { get; init; }

    public Guid? LogoMediaAssetId { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertBrandTranslationRequest
{
    [Required]
    [StringLength(256)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }
}
