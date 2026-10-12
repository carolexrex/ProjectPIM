using Platform.Domain.Common;

namespace Platform.Domain.Catalog.Inventory;

public sealed class InventoryBalance
{
    private InventoryBalance()
    {
        Id = Guid.Empty;
        RowVersion = string.Empty;
    }

    public InventoryBalance(
        Guid id,
        Guid inventoryLocationId,
        Guid variantId,
        decimal onHandQuantity,
        decimal reservedQuantity,
        decimal incomingQuantity,
        bool backorderable,
        DateTime updatedAtUtc)
    {
        Id = id;
        InventoryLocationId = inventoryLocationId;
        VariantId = variantId;
        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
        IncomingQuantity = incomingQuantity;
        Backorderable = backorderable;
        UpdatedAtUtc = updatedAtUtc;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    public Guid Id { get; private set; }
    public Guid InventoryLocationId { get; private set; }
    public Guid VariantId { get; private set; }
    public decimal OnHandQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal IncomingQuantity { get; private set; }
    public bool Backorderable { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string RowVersion { get; private set; }
    public decimal AvailableQuantity => OnHandQuantity - ReservedQuantity;

    public void UpdateSnapshot(
        decimal onHandQuantity,
        decimal reservedQuantity,
        decimal incomingQuantity,
        bool backorderable,
        string? rowVersion)
    {
        EnsureRowVersion(rowVersion);
        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
        IncomingQuantity = incomingQuantity;
        Backorderable = backorderable;
        Touch();
    }

    public InventoryTransaction Adjust(
        string type,
        decimal quantityDelta,
        string referenceType,
        Guid referenceId,
        DateTime occurredAtUtc)
    {
        OnHandQuantity += quantityDelta;
        Touch();

        return new InventoryTransaction(
            Guid.NewGuid(),
            InventoryLocationId,
            VariantId,
            NormalizeRequired(type),
            quantityDelta,
            NormalizeRequired(referenceType),
            referenceId,
            occurredAtUtc);
    }

    /// <summary>
    /// Holds <paramref name="quantity"/> for an order. On-hand stock is unchanged. A non-backorderable
    /// balance can never reserve beyond its available quantity; a backorderable balance may, which is
    /// what makes available quantity negative.
    /// </summary>
    public InventoryTransaction Reserve(decimal quantity, Guid orderId, DateTime occurredAtUtc)
    {
        EnsurePositive(quantity);
        if (!Backorderable && quantity > AvailableQuantity)
        {
            throw new InvalidOperationException(
                $"Cannot reserve {quantity} from a non-backorderable balance with {AvailableQuantity} available.");
        }

        ReservedQuantity += quantity;
        Touch();
        return CreateTransaction(InventoryTransactionTypes.Reservation, 0m, quantity, orderId, occurredAtUtc);
    }

    /// <summary>
    /// Returns a previously reserved quantity to available stock (order cancellation).
    /// </summary>
    public InventoryTransaction ReleaseReservation(decimal quantity, Guid orderId, DateTime occurredAtUtc)
    {
        EnsurePositive(quantity);
        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                $"Cannot release {quantity}; only {ReservedQuantity} is reserved on the balance.");
        }

        ReservedQuantity -= quantity;
        Touch();
        return CreateTransaction(InventoryTransactionTypes.ReservationRelease, 0m, -quantity, orderId, occurredAtUtc);
    }

    /// <summary>
    /// True when a reserved quantity can be shipped: it is still reserved and physically on hand.
    /// </summary>
    public bool CanShipReservation(decimal quantity)
    {
        return quantity > 0m && quantity <= ReservedQuantity && quantity <= OnHandQuantity;
    }

    /// <summary>
    /// Consumes a reservation when the order is fulfilled: on-hand and reserved stock both decrease,
    /// so available quantity is unchanged. Requires the stock to be physically on hand.
    /// </summary>
    public InventoryTransaction ShipReservation(decimal quantity, Guid orderId, DateTime occurredAtUtc)
    {
        EnsurePositive(quantity);
        if (!CanShipReservation(quantity))
        {
            throw new InvalidOperationException(
                $"Cannot ship {quantity}; the balance has {OnHandQuantity} on hand and {ReservedQuantity} reserved.");
        }

        OnHandQuantity -= quantity;
        ReservedQuantity -= quantity;
        Touch();
        return CreateTransaction(InventoryTransactionTypes.Shipment, -quantity, -quantity, orderId, occurredAtUtc);
    }

    private InventoryTransaction CreateTransaction(
        string type,
        decimal onHandDelta,
        decimal reservedDelta,
        Guid orderId,
        DateTime occurredAtUtc)
    {
        return new InventoryTransaction(
            Guid.NewGuid(),
            InventoryLocationId,
            VariantId,
            type,
            onHandDelta,
            InventoryTransactionTypes.OrderReference,
            orderId,
            occurredAtUtc,
            reservedDelta);
    }

    private static void EnsurePositive(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new InvalidOperationException("Inventory quantity must be positive.");
        }
    }

    private void EnsureRowVersion(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return;
        }

        if (!string.Equals(RowVersion, rowVersion, StringComparison.Ordinal))
        {
            throw new ConcurrencyException("The inventory balance has changed since it was loaded.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private static string NormalizeRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
