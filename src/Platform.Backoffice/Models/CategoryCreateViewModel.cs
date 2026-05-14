using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class CategoryCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    public Guid? ParentCategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; } = 10;

    public IReadOnlyList<CategoryLookupOptionViewModel> ParentOptions { get; set; } = [];
}
