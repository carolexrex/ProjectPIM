using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class PriceListEntryCreateViewModel
{
    public Guid PriceListId { get; set; }
    public Guid? EntryId { get; set; }

    [Required]
    public Guid? TargetId { get; set; }

    [Range(1, int.MaxValue)]
    public int MinQuantity { get; set; } = 1;

    public decimal Amount { get; set; }

    public decimal? CompareAtAmount { get; set; }

    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<VariantLookupOptionViewModel> VariantOptions { get; set; } = [];
}
