using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class BrandTranslationUpsertViewModel
{
    public Guid BrandId { get; set; }

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string CultureCode { get; set; } = "en-GB";

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }
}
