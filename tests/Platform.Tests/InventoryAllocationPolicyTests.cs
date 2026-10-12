using Platform.Domain.Catalog.Inventory;

namespace Platform.Tests;

public sealed class InventoryAllocationPolicyTests
{
    private static readonly Guid VariantId = Guid.NewGuid();

    [Fact]
    public void Allocate_TakesStockByPriorityThenLocationCodeThenBalanceId()
    {
        var lowPriority = Source("A-LOW", priority: 5, onHand: 10m);
        var secondByCode = Source("B-WH", priority: 1, onHand: 2m);
        var firstByCode = Source("A-WH", priority: 1, onHand: 3m);

        var outcome = InventoryAllocationPolicy.Allocate(7m, [lowPriority, secondByCode, firstByCode]);

        Assert.True(outcome.IsSatisfied);
        Assert.Equal(
            [(firstByCode.Balance.Id, 3m), (secondByCode.Balance.Id, 2m), (lowPriority.Balance.Id, 2m)],
            outcome.Allocations.Select(x => (x.Balance.Id, x.Quantity)).ToArray());
        Assert.All(outcome.Allocations, x => Assert.Equal(0m, x.BackorderedQuantity));
    }

    [Fact]
    public void Allocate_IgnoresNegativeAvailabilityAndRejectsShortfallWithoutBackorder()
    {
        var overReserved = Source("A", priority: 0, onHand: 1m, reserved: 4m);
        var stocked = Source("B", priority: 1, onHand: 2m);

        var outcome = InventoryAllocationPolicy.Allocate(3m, [overReserved, stocked]);

        Assert.False(outcome.IsSatisfied);
        Assert.True(outcome.HasSource);
        Assert.Equal(2m, outcome.AvailableQuantity);
        Assert.Empty(outcome.Allocations);
    }

    [Fact]
    public void Allocate_BackordersRemainderOnFirstBackorderableSourceAndMergesPerBalance()
    {
        var plain = Source("A", priority: 0, onHand: 1m);
        var backorderable = Source("B", priority: 1, onHand: 0.5m, backorderable: true);
        var laterBackorderable = Source("C", priority: 2, onHand: 0m, backorderable: true);

        var outcome = InventoryAllocationPolicy.Allocate(4m, [laterBackorderable, backorderable, plain]);

        Assert.True(outcome.IsSatisfied);
        Assert.Equal(2, outcome.Allocations.Count);
        Assert.Equal((plain.Balance.Id, 1m, 0m), Tuple(outcome.Allocations[0]));
        Assert.Equal((backorderable.Balance.Id, 3m, 2.5m), Tuple(outcome.Allocations[1]));
    }

    [Fact]
    public void Allocate_ReportsMissingSource()
    {
        var outcome = InventoryAllocationPolicy.Allocate(1m, []);

        Assert.False(outcome.IsSatisfied);
        Assert.False(outcome.HasSource);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0.0001", true)]
    [InlineData("2.12345", false)]
    public void HasSupportedScale_MatchesFourDecimalStorage(string quantity, bool expected)
    {
        Assert.Equal(expected, InventoryAllocationPolicy.HasSupportedScale(decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Balance_ReserveReleaseAndShipKeepOnHandAndReservedConsistent()
    {
        var orderId = Guid.NewGuid();
        var balance = new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), VariantId, 5m, 1m, 0m, false, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => balance.Reserve(4.5m, orderId, DateTime.UtcNow));
        var reservation = balance.Reserve(4m, orderId, DateTime.UtcNow);
        Assert.Equal((5m, 5m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal((0m, 4m, "Reservation", "Order", orderId), (reservation.QuantityDelta, reservation.ReservedQuantityDelta, reservation.Type, reservation.ReferenceType, reservation.ReferenceId));

        var release = balance.ReleaseReservation(1.5m, orderId, DateTime.UtcNow);
        Assert.Equal((5m, 3.5m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal(-1.5m, release.ReservedQuantityDelta);

        var shipment = balance.ShipReservation(2.5m, orderId, DateTime.UtcNow);
        Assert.Equal((2.5m, 1m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal((-2.5m, -2.5m, "Shipment"), (shipment.QuantityDelta, shipment.ReservedQuantityDelta, shipment.Type));

        Assert.Throws<InvalidOperationException>(() => balance.ReleaseReservation(1.5m, orderId, DateTime.UtcNow));
    }

    [Fact]
    public void Balance_ShipRequiresPhysicalStockEvenWhenBackorderable()
    {
        var balance = new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), VariantId, 1m, 0m, 0m, true, DateTime.UtcNow);
        balance.Reserve(3m, Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(-2m, balance.AvailableQuantity);
        Assert.False(balance.CanShipReservation(3m));
        Assert.Throws<InvalidOperationException>(() => balance.ShipReservation(3m, Guid.NewGuid(), DateTime.UtcNow));
        Assert.True(balance.CanShipReservation(1m));
    }

    [Fact]
    public void Allocation_SettlesExactlyOnce()
    {
        var allocation = new InventoryAllocation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), VariantId, 2m, 0m, DateTime.UtcNow);

        allocation.MarkReleased(DateTime.UtcNow);

        Assert.False(allocation.IsOutstanding);
        Assert.Throws<InvalidOperationException>(() => allocation.MarkReleased(DateTime.UtcNow));
        Assert.Throws<InvalidOperationException>(() => allocation.MarkShipped(DateTime.UtcNow));
    }

    private static (Guid, decimal, decimal) Tuple(PlannedInventoryAllocation allocation)
    {
        return (allocation.Balance.Id, allocation.Quantity, allocation.BackorderedQuantity);
    }

    private static InventoryAllocationSource Source(string code, int priority, decimal onHand, decimal reserved = 0m, bool backorderable = false)
    {
        return new InventoryAllocationSource(
            new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), VariantId, onHand, reserved, 0m, backorderable, DateTime.UtcNow),
            priority,
            code);
    }
}
