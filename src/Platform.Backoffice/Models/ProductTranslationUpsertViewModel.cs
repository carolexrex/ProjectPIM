using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ProductTranslationUpsertViewModel
{
    public Guid ProductId { get; set; }

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string CultureCode { get; set; } = "en-GB";

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? ShortDescription { get; set; }

    public string? LongDescription { get; set; }

    [StringLength(256)]
    public string? SeoTitle { get; set; }

    [StringLength(512)]
    public string? SeoDescription { get; set; }
}
