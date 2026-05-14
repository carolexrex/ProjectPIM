using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ProductAttributeUpdateViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Scope { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string DataType { get; set; } = string.Empty;

    public bool IsVariantDefining { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsRequired { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string OptionsText { get; set; } = string.Empty;

    public IReadOnlyList<string> ScopeOptions { get; set; } = [];
    public IReadOnlyList<string> DataTypeOptions { get; set; } = [];
}
