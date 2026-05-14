using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class MarketCurrenciesFormViewModel
{
    public Guid MarketId { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; set; } = string.Empty;

    [Required]
    public string CurrencyCodesCsv { get; set; } = string.Empty;

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
