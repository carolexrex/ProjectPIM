using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class VariantCreateViewModel
{
    public Guid ProductId { get; set; }

    [Required]
    [StringLength(64)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Ean { get; set; }

    [StringLength(64)]
    public string? Mpn { get; set; }

    [StringLength(64)]
    public string? Barcode { get; set; }

    public Guid ProductStatusDefinitionId { get; set; }
    public bool IsDefaultVariant { get; set; } = true;
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }

    public IReadOnlyList<StatusOptionViewModel> StatusOptions { get; set; } = [];
    public List<VariantAttributeEditorViewModel> AttributeEditors { get; set; } = [];
}
