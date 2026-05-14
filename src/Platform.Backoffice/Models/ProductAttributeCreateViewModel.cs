using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ProductAttributeCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Scope { get; set; } = "Variant";

    [Required]
    [StringLength(32)]
    public string DataType { get; set; } = "Select";

    public bool IsVariantDefining { get; set; } = true;
    public bool IsFilterable { get; set; } = true;
    public bool IsRequired { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; } = 10;

    public string OptionsText { get; set; } = string.Empty;

    public IReadOnlyList<string> ScopeOptions { get; set; } = [];
    public IReadOnlyList<string> DataTypeOptions { get; set; } = [];
}
