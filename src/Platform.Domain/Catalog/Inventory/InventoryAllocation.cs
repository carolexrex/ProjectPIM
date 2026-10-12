namespace Platform.Domain.Catalog.Inventory;

/// <summary>
/// Stock held for one order on one inventory balance. Created when the order is placed and settled
/// exactly once: released when the order is cancelled, or shipped when the order is completed.
/// </summary>
public sealed class InventoryAllocation
{
    public const string ReservedStatus = "Reserved";
    public const string ReleasedStatus = "Released";
    public const string ShippedStatus = "Shipped";

    private InventoryAllocation()
    {
        Id = Guid.Empty;
        Status = string.Empty;
        RowVersion = string.Empty;
    }

    public InventoryAllocation(
        Guid id,
        Guid orderId,
        Guid inventoryBalanceId,
        Guid inventoryLocationId,
        Guid variantId,
        decimal quantity,
        decimal backorderedQuantity,
        DateTime createdAtUtc)
    {
        if (quantity <= 0m)
        {
            throw new InvalidOperationException("Allocation quantity must be positive.");
        }

        if (backorderedQuantity < 0m || backorderedQuantity > quantity)
        {
            throw new InvalidOperationException("Backordered quantity must be between zero and the allocated quantity.");
        }

        Id = id;
        OrderId = orderId;
        InventoryBalanceId = inventoryBalanceId;
        InventoryLocationId = inventoryLocationId;
        VariantId = variantId;
        Quantity = quantity;
        BackorderedQuantity = backorderedQuantity;
        Status = ReservedStatus;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = NewRowVersion();
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid InventoryBalanceId { get; private set; }
    public Guid InventoryLocationId { get; private set; }
    public Guid VariantId { get; private set; }
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Portion of <see cref="Quantity"/> that exceeded available stock when the order was placed and
    /// was accepted only because the balance is backorderable.
    /// </summary>
    public decimal BackorderedQuantity { get; private set; }

    public string Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public bool IsOutstanding => string.Equals(Status, ReservedStatus, StringComparison.Ordinal);

    public void MarkReleased(DateTime occurredAtUtc)
    {
        Settle(ReleasedStatus, occurredAtUtc);
    }

    public void MarkShipped(DateTime occurredAtUtc)
    {
        Settle(ShippedStatus, occurredAtUtc);
    }

    private void Settle(string status, DateTime occurredAtUtc)
    {
        if (!IsOutstanding)
        {
            throw new InvalidOperationException($"Allocation {Id} is already {Status}.");
        }

        Status = status;
        UpdatedAtUtc = occurredAtUtc;
        RowVersion = NewRowVersion();
    }

    private static string NewRowVersion()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
