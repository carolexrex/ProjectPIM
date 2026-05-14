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
