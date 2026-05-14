using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class InventoryLocationCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [StringLength(2, MinimumLength = 2)]
    public string? CountryCode { get; set; }

    public IReadOnlyList<string> TypeOptions { get; set; } = [];
}
