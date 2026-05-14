using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ProductRelationCreateViewModel
{
    public Guid ProductId { get; set; }

    [Required]
    public Guid TargetProductId { get; set; }

    [Required]
    [StringLength(32)]
    public string RelationType { get; set; } = "RelatedProduct";

    [Range(0.0001, 1000000)]
    public decimal? Quantity { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<string> RelationTypeOptions { get; set; } = [];
    public IReadOnlyList<ProductLookupOptionViewModel> TargetProductOptions { get; set; } = [];
}
