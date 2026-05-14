namespace Platform.Backoffice.Models;

public sealed class VariantDetailsPageViewModel
{
    public VariantUpdateViewModel Variant { get; init; } = new();
    public Guid ProductId { get; init; }
    public IReadOnlyList<Platform.Contracts.Catalog.Variants.VariantMediaDto> Media { get; init; } = [];
    public Platform.Contracts.Catalog.Inventory.VariantInventorySnapshotDto? InventorySnapshot { get; init; }
    public VariantMediaCreateViewModel MediaForm { get; init; } = new();
}
