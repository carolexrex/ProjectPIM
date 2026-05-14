using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Integrations;

public sealed class CreateBrandExportJobRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }
}

public sealed class CreateStorefrontProjectionRebuildJobRequest
{
}

public sealed class CreateProductExportJobRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? ProductStatusCode { get; init; }
    public Guid? BrandId { get; init; }
    public bool? HasVariants { get; init; }
}

public sealed class CreateProductImportJobRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<CreateProductImportJobItemRequest> Products { get; init; } = [];
}

public sealed class CreateProductImportJobItemRequest
{
    [Required]
    [StringLength(64)]
    public string ProductType { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string ProductNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; init; } = string.Empty;

    [StringLength(64)]
    public string? BrandCode { get; init; }

    [Required]
    [StringLength(64)]
    public string ProductStatusCode { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string TaxCategoryCode { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string UnitOfMeasure { get; init; } = string.Empty;

    public bool HasVariants { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Length { get; init; }
    public decimal? Width { get; init; }
    public decimal? Height { get; init; }
    public IReadOnlyList<string> CategoryCodes { get; init; } = [];

    [Required]
    public IReadOnlyList<CreateProductImportJobAttributeValueRequest> AttributeValues { get; init; } = [];

    public IReadOnlyList<CreateProductImportJobTranslationRequest> Translations { get; init; } = [];
}

public sealed class CreateProductImportJobAttributeValueRequest
{
    [Required]
    [StringLength(64)]
    public string ProductAttributeCode { get; init; } = string.Empty;

    [StringLength(64)]
    public string? AttributeOptionCode { get; init; }

    [StringLength(256)]
    public string? ValueText { get; init; }
}

public sealed class CreateProductImportJobTranslationRequest
{
    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string CultureCode { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1024)]
    public string? ShortDescription { get; init; }

    public string? LongDescription { get; init; }

    [StringLength(256)]
    public string? SeoTitle { get; init; }

    [StringLength(512)]
    public string? SeoDescription { get; init; }
}

public sealed class CreateBrandImportJobRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<CreateBrandImportJobItemRequest> Brands { get; init; } = [];
}

public sealed class CreateBrandImportJobItemRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [StringLength(1024)]
    public string? WebsiteUrl { get; init; }

    public Guid? LogoMediaAssetId { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    public IReadOnlyList<CreateBrandImportJobTranslationRequest> Translations { get; init; } = [];
}

public sealed class CreateBrandImportJobTranslationRequest
{
    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string CultureCode { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }
}
