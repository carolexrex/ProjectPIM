using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class PriceListCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; set; } = "SEK";

    public bool VatIncluded { get; set; } = true;
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
}
