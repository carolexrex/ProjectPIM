using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Catalog.Categories;

public sealed class CreateCategoryRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    public Guid? ParentCategoryId { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
}

public sealed class UpdateCategoryRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    public Guid? ParentCategoryId { get; init; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertCategoryTranslationRequest
{
    [Required]
    [StringLength(256)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }
}
