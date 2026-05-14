using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ProductUpdateViewModel
{
    public Guid Id { get; set; }
    public string ProductNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string ProductType { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }
    public Guid ProductStatusDefinitionId { get; set; }

    [Required]
    [StringLength(64)]
    public string TaxCategoryCode { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    public bool HasVariants { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public IReadOnlyList<Guid> SelectedCategoryIds { get; set; } = [];
    public List<ProductAttributeEditorViewModel> AttributeEditors { get; set; } = [];

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public IReadOnlyList<StatusOptionViewModel> StatusOptions { get; set; } = [];
    public IReadOnlyList<BrandLookupOptionViewModel> BrandOptions { get; set; } = [];
    public IReadOnlyList<CategoryLookupOptionViewModel> CategoryOptions { get; set; } = [];
}
