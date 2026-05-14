using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class InventoryBalanceUpsertViewModel
{
    public Guid InventoryLocationId { get; set; }

    [Required]
    public Guid? VariantId { get; set; }

    public decimal OnHandQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal IncomingQuantity { get; set; }
    public bool Backorderable { get; set; }
    public string? RowVersion { get; set; }

    public IReadOnlyList<VariantLookupOptionViewModel> VariantOptions { get; set; } = [];
}
