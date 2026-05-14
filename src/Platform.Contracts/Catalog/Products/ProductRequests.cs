using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Products;

public sealed class CreateProductRequest
{
    public sealed class ProductAttributeValueRequest
    {
        [NotEmptyGuid]
        public Guid ProductAttributeId { get; init; }

        public Guid? AttributeOptionId { get; init; }

        [StringLength(256)]
        public string? ValueText { get; init; }
    }

    [Required]
    [StringLength(64)]
    public string ProductType { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string ProductNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; init; } = string.Empty;

    public Guid? BrandId { get; init; }

    [NotEmptyGuid]
    public Guid ProductStatusDefinitionId { get; init; }

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
    public IReadOnlyList<Guid> CategoryIds { get; init; } = [];
    [Required]
    public IReadOnlyList<ProductAttributeValueRequest> AttributeValues { get; init; } = [];
}

public sealed class UpdateProductRequest
{
    [Required]
    [StringLength(64)]
    public string ProductType { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; init; } = string.Empty;

    public Guid? BrandId { get; init; }

    [NotEmptyGuid]
    public Guid ProductStatusDefinitionId { get; init; }

    [Required]
    [StringLength(64)]
    public string TaxCategoryCode { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string UnitOfMeasure { get; init; } = string.Empty;

    public decimal? Weight { get; init; }
    public decimal? Length { get; init; }
    public decimal? Width { get; init; }
    public decimal? Height { get; init; }
    public IReadOnlyList<Guid> CategoryIds { get; init; } = [];
    [Required]
    public IReadOnlyList<CreateProductRequest.ProductAttributeValueRequest> AttributeValues { get; init; } = [];

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AssignProductStatusRequest
{
    [NotEmptyGuid]
    public Guid ProductStatusDefinitionId { get; init; }

    [StringLength(1024)]
    public string? Comment { get; init; }
}

public sealed class UpsertProductRelationRequest
{
    [NotEmptyGuid]
    public Guid TargetProductId { get; init; }

    [Required]
    [StringLength(32)]
    public string RelationType { get; init; } = string.Empty;

    [Range(0.0001, 1000000)]
    public decimal? Quantity { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class RemoveProductRelationRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertProductTranslationRequest
{
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
