namespace Platform.Domain.Catalog.Inventory;

public sealed class InventoryTransaction
{
    private InventoryTransaction()
    {
        Id = Guid.Empty;
        Type = string.Empty;
        ReferenceType = string.Empty;
    }

    public InventoryTransaction(
        Guid id,
        Guid inventoryLocationId,
        Guid variantId,
        string type,
        decimal quantityDelta,
        string referenceType,
        Guid referenceId,
        DateTime occurredAtUtc)
    {
        Id = id;
        InventoryLocationId = inventoryLocationId;
        VariantId = variantId;
        Type = type;
        QuantityDelta = quantityDelta;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid InventoryLocationId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Type { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public string ReferenceType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
