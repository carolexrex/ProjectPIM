using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class InventoryLocationUpdateViewModel
{
    public Guid Id { get; set; }

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

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public IReadOnlyList<string> TypeOptions { get; set; } = [];
}
