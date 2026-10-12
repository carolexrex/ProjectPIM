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
        DateTime occurredAtUtc,
        decimal reservedQuantityDelta = 0m)
    {
        Id = id;
        InventoryLocationId = inventoryLocationId;
        VariantId = variantId;
        Type = type;
        QuantityDelta = quantityDelta;
        ReservedQuantityDelta = reservedQuantityDelta;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid InventoryLocationId { get; private set; }
    public Guid VariantId { get; private set; }
    public string Type { get; private set; }
    /// <summary>Change to on-hand stock.</summary>
    public decimal QuantityDelta { get; private set; }
    /// <summary>Change to reserved stock (order reservations, releases, and shipments).</summary>
    public decimal ReservedQuantityDelta { get; private set; }
    public string ReferenceType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
