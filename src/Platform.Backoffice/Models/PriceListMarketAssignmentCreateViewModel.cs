using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class PriceListMarketAssignmentCreateViewModel
{
    public Guid PriceListId { get; set; }

    [Required]
    public Guid? MarketId { get; set; }

    [Range(0, int.MaxValue)]
    public int Priority { get; set; }

    public bool IsBasePriceList { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<MarketLookupOptionViewModel> MarketOptions { get; set; } = [];
}
