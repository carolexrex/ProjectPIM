using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class MarketProductAssignmentCreateViewModel
{
    public Guid MarketId { get; set; }

    [Required]
    public Guid? ProductId { get; set; }

    [Required]
    [StringLength(32)]
    public string Status { get; set; } = "Active";

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<string> StatusOptions { get; set; } = ["Active", "Inactive"];
    public IReadOnlyList<ProductLookupOptionViewModel> ProductOptions { get; set; } = [];
}
