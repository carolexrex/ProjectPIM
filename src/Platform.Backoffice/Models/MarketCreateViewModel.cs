using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class MarketCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; set; } = "SEK";

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string DefaultCulture { get; set; } = "sv-SE";

    [Required]
    [StringLength(32)]
    public string VatMode { get; set; } = "Gross";

    public IReadOnlyList<string> VatModeOptions { get; set; } = ["Gross", "Net"];
}
