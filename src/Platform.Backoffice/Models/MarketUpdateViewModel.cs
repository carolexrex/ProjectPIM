using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class MarketUpdateViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; set; } = string.Empty;

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string DefaultCulture { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string VatMode { get; set; } = "Gross";

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public IReadOnlyList<string> VatModeOptions { get; set; } = ["Gross", "Net"];
}
