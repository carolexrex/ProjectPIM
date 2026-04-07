using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Variants;

public sealed class VariantAttributeValueRequest
{
    [NotEmptyGuid]
    public Guid ProductAttributeId { get; init; }

    public Guid? AttributeOptionId { get; init; }

    [StringLength(256)]
    public string? ValueText { get; init; }
}

public sealed class CreateVariantRequest
{
    [Required]
    [StringLength(64)]
    public string Sku { get; init; } = string.Empty;

    [StringLength(64)]
    public string? Ean { get; init; }

    [StringLength(64)]
    public string? Mpn { get; init; }

    [StringLength(64)]
    public string? Barcode { get; init; }

    [NotEmptyGuid]
    public Guid ProductStatusDefinitionId { get; init; }

    public bool IsDefaultVariant { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Length { get; init; }
    public decimal? Width { get; init; }
    public decimal? Height { get; init; }

    [Required]
    public IReadOnlyList<VariantAttributeValueRequest> AttributeValues { get; init; } = [];
}

public sealed class UpdateVariantRequest
{
    [Required]
    [StringLength(64)]
    public string Sku { get; init; } = string.Empty;

    [StringLength(64)]
    public string? Ean { get; init; }

    [StringLength(64)]
    public string? Mpn { get; init; }

    [StringLength(64)]
    public string? Barcode { get; init; }

    [NotEmptyGuid]
    public Guid ProductStatusDefinitionId { get; init; }

    public bool IsDefaultVariant { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Length { get; init; }
    public decimal? Width { get; init; }
    public decimal? Height { get; init; }

    [Required]
    public IReadOnlyList<VariantAttributeValueRequest> AttributeValues { get; init; } = [];

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AssignVariantStatusRequest
{
    [NotEmptyGuid]
    public Guid ProductStatusDefinitionId { get; init; }

    [StringLength(1024)]
    public string? Comment { get; init; }
}
