using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class InventoryAdjustmentCreateViewModel
{
    public Guid InventoryLocationId { get; set; }

    [Required]
    public Guid? VariantId { get; set; }

    [Required]
    public string Type { get; set; } = "Adjustment";

    public decimal QuantityDelta { get; set; }

    [Required]
    public string ReferenceType { get; set; } = "ManualAdjustment";

    public Guid? ReferenceId { get; set; }

    public IReadOnlyList<string> TypeOptions { get; set; } = [];
    public IReadOnlyList<VariantLookupOptionViewModel> VariantOptions { get; set; } = [];
}
