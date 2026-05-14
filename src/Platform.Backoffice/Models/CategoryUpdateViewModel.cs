using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class CategoryUpdateViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    public Guid? ParentCategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public IReadOnlyList<CategoryLookupOptionViewModel> ParentOptions { get; set; } = [];
}
