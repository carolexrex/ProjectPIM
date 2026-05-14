using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class MarketCulturesFormViewModel
{
    public Guid MarketId { get; set; }

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string DefaultCulture { get; set; } = string.Empty;

    [Required]
    public string CultureCodesCsv { get; set; } = string.Empty;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
