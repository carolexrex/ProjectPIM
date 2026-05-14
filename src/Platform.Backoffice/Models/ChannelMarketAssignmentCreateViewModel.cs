using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ChannelMarketAssignmentCreateViewModel
{
    public Guid ChannelId { get; set; }

    [Required]
    public Guid? MarketId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
    public IReadOnlyList<MarketLookupOptionViewModel> MarketOptions { get; set; } = [];
}
