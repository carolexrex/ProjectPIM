using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ProductCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string ProductType { get; set; } = "Hardware";

    [Required]
    [StringLength(64)]
    public string ProductNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }
    public Guid ProductStatusDefinitionId { get; set; }

    [Required]
    [StringLength(64)]
    public string TaxCategoryCode { get; set; } = "STANDARD";

    [Required]
    [StringLength(32)]
    public string UnitOfMeasure { get; set; } = "pcs";

    public bool HasVariants { get; set; } = true;
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public IReadOnlyList<Guid> SelectedCategoryIds { get; set; } = [];
    public List<ProductAttributeEditorViewModel> AttributeEditors { get; set; } = [];

    public IReadOnlyList<StatusOptionViewModel> StatusOptions { get; set; } = [];
    public IReadOnlyList<BrandLookupOptionViewModel> BrandOptions { get; set; } = [];
    public IReadOnlyList<CategoryLookupOptionViewModel> CategoryOptions { get; set; } = [];
}
