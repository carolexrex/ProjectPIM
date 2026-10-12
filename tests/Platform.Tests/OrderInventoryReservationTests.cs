using System.Text.Json;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Orders.Commands;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Common;
using Platform.Domain.Integrations;

namespace Platform.Tests;

/// <summary>
/// Order inventory reservation lifecycle on the in-memory provider: placement reserves, cancellation
/// releases, completion ships, and every rejected step leaves no partial changes.
/// </summary>
public sealed class OrderInventoryReservationTests
{
    private static readonly Guid SeededBalanceId = Guid.Parse("66000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededOrderId = Guid.Parse("79000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededCartId = Guid.Parse("78000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task StorefrontCheckout_AggregatesDuplicateFractionalLinesIntoOneReservation()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 5m);
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((variantId, 1.5m), (variantId, 2.25m));
        var outboxBefore = rig.Store.OutboxMessages.Keys.ToHashSet();

        var checkout = await CheckoutAsync(rig, created);

        Assert.Equal(StorefrontContextResolutionStatus.Success, checkout.Status);
        Assert.Equal((5m, 3.75m), (balance.OnHandQuantity, balance.ReservedQuantity));
        var allocation = Assert.Single(rig.Store.InventoryAllocations.Values);
        Assert.Equal((checkout.Order!.Id, balance.Id, 3.75m, 0m, "Reserved"), (allocation.OrderId, allocation.InventoryBalanceId, allocation.Quantity, allocation.BackorderedQuantity, allocation.Status));
        var transaction = Assert.Single(rig.Store.InventoryTransactions.Values, x => x.Type == InventoryTransactionTypes.Reservation);
        Assert.Equal((0m, 3.75m, "Order", checkout.Order.Id), (transaction.QuantityDelta, transaction.ReservedQuantityDelta, transaction.ReferenceType, transaction.ReferenceId));
        var refresh = Assert.Single(RefreshPayloads(rig, outboxBefore));
        Assert.Equal("OrderInventoryReserved", refresh.Reason);
        Assert.Equal([variantId], refresh.VariantIds);
        Assert.Equal("Converted", rig.Store.Carts[created.Cart!.Id].Status);
    }

    [Fact]
    public async Task Placement_SplitsByLocationPriorityAndIgnoresOtherMarketsAndArchivedLocations()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var main = MainBalance(rig, variantId);
        SetStock(main, onHand: 2m);
        var secondary = rig.AddBalance(rig.AddLocation(VatPricingTestRig.SeMarketId, "SE-B", priority: 1).Id, variantId, 5m);
        var fiMarket = rig.AddMarket(Guid.NewGuid(), "FI", "EUR", "fi-FI");
        var otherMarket = rig.AddBalance(rig.AddLocation(fiMarket.Id, "FI-WH").Id, variantId, 100m);
        var archivedLocation = rig.AddLocation(VatPricingTestRig.SeMarketId, "SE-OLD", priority: -1);
        archivedLocation.Archive();
        var archived = rig.AddBalance(archivedLocation.Id, variantId, 100m);

        var order = await rig.Orders.CreateAsync(ManualOrder(variantId, 4m), "tester", CancellationToken.None);

        Assert.Equal(2m, main.ReservedQuantity);
        Assert.Equal(2m, secondary.ReservedQuantity);
        Assert.Equal(0m, otherMarket.ReservedQuantity);
        Assert.Equal(0m, archived.ReservedQuantity);
        var allocated = rig.Store.InventoryAllocations.Values.Where(x => x.OrderId == order.Id).ToDictionary(x => x.InventoryBalanceId, x => x.Quantity);
        Assert.Equal(2, allocated.Count);
        Assert.Equal((2m, 2m), (allocated[main.Id], allocated[secondary.Id]));

        var tooMuch = await Assert.ThrowsAsync<RequestValidationException>(() =>
            rig.Orders.CreateAsync(ManualOrder(variantId, 4m), "tester", CancellationToken.None));
        Assert.Contains("requested 4, available 3", Assert.Single(tooMuch.Errors[$"VariantId.{variantId}"]));
    }

    [Fact]
    public async Task Placement_RejectsVariantWithoutStockLocationInMarket()
    {
        var rig = new VatPricingTestRig();
        var fiMarket = rig.AddMarket(Guid.NewGuid(), "FI", "EUR", "fi-FI");
        rig.AddVatRule(fiMarket.Id, "STANDARD", 0.255m);
        var fiList = rig.AddPriceList("FI_BASE", "EUR", vatIncluded: true, fiMarket.Id, priority: 0);
        fiList.UpsertEntry(null, "Variant", VatPricingTestRig.SeededVariantId, 1, 125.50m, null, null, null, fiList.RowVersion);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            rig.Orders.CreateAsync(ManualOrder(VatPricingTestRig.SeededVariantId, 1m, fiMarket.Id, "EUR"), "tester", CancellationToken.None));

        Assert.Contains("not stocked at any active inventory location assigned to market 'FI'", Assert.Single(exception.Errors.Values.SelectMany(x => x)));
        Assert.Empty(rig.Store.InventoryAllocations);
        Assert.Single(rig.Store.Orders);
    }

    [Fact]
    public async Task Checkout_WithOneShortLine_LeavesNoPartialChanges()
    {
        var rig = new VatPricingTestRig();
        var plenty = rig.AddProduct(1, "STANDARD", 100m);
        var scarce = rig.AddProduct(2, "STANDARD", 50m);
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((plenty, 2m), (scarce, 3m));

        // Stock drops after the cart was built; the projection still says 1000 available.
        await rig.Inventory.AdjustInventoryAsync(
            new AdjustInventoryCommand(VatPricingTestRig.MainLocationId, scarce, "Adjustment", -999m, "Recount", null),
            CancellationToken.None);
        var snapshot = Snapshot(rig);

        var checkout = await CheckoutAsync(rig, created);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, checkout.Status);
        Assert.Equal(
            $"Insufficient stock for SKU-VAT-2-STD in market 'SE': requested 3, available 1.",
            Assert.Single(checkout.Errors[$"VariantId.{scarce}"]));
        Assert.DoesNotContain($"VariantId.{plenty}", checkout.Errors.Keys);
        Assert.Equal(snapshot, Snapshot(rig));
        var cart = rig.Store.Carts[created.Cart!.Id];
        Assert.Equal(("Active", created.Cart.RowVersion), (cart.Status, cart.RowVersion));

        // Nothing was committed, so the same row version can retry once stock returns.
        await rig.Inventory.AdjustInventoryAsync(
            new AdjustInventoryCommand(VatPricingTestRig.MainLocationId, scarce, "Adjustment", 2m, "Restock", null),
            CancellationToken.None);
        var retried = await CheckoutAsync(rig, created);
        Assert.Equal(StorefrontContextResolutionStatus.Success, retried.Status);
        Assert.Equal(3m, MainBalance(rig, scarce).ReservedQuantity);
    }

    [Fact]
    public async Task Placement_BackordersOnlyBackorderableStockAndCompletionWaitsForPhysicalStock()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 1m, backorderable: true);

        var order = await rig.Orders.CreateAsync(ManualOrder(variantId, 3m), "tester", CancellationToken.None);

        Assert.Equal((1m, 3m, -2m), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
        var allocation = Assert.Single(rig.Store.InventoryAllocations.Values);
        Assert.Equal((3m, 2m), (allocation.Quantity, allocation.BackorderedQuantity));

        var processing = await ChangeStatusAsync(rig, order.Id, "Processing");
        var rejected = await Assert.ThrowsAsync<RequestValidationException>(() => ChangeStatusAsync(rig, order.Id, "Completed"));
        Assert.Contains("Cannot complete the order: location 'MAIN' has 1 on hand", Assert.Single(rejected.Errors[$"VariantId.{variantId}"]));
        Assert.Equal(("Processing", processing), (rig.Store.Orders[order.Id].Status, rig.Store.Orders[order.Id].RowVersion));
        Assert.Equal((1m, 3m), (balance.OnHandQuantity, balance.ReservedQuantity));

        await rig.Inventory.AdjustInventoryAsync(
            new AdjustInventoryCommand(VatPricingTestRig.MainLocationId, variantId, "Adjustment", 2m, "PurchaseReceipt", null),
            CancellationToken.None);
        await ChangeStatusAsync(rig, order.Id, "Completed");

        Assert.Equal((0m, 0m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal("Shipped", allocation.Status);
    }

    [Fact]
    public async Task Placement_NeverReservesBeyondNonBackorderableStock()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 2m);

        await rig.Orders.CreateAsync(ManualOrder(variantId, 1.5m), "tester", CancellationToken.None);
        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            rig.Orders.CreateAsync(ManualOrder(variantId, 1m), "tester", CancellationToken.None));

        Assert.Contains("requested 1, available 0.5", Assert.Single(exception.Errors[$"VariantId.{variantId}"]));
        Assert.Equal(1.5m, balance.ReservedQuantity);
    }

    [Fact]
    public async Task Placement_RejectsQuantitiesFinerThanStoredScale()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            rig.Orders.CreateAsync(ManualOrder(variantId, 1.00005m), "tester", CancellationToken.None));

        Assert.Contains("more than 4 decimal places", Assert.Single(exception.Errors[$"VariantId.{variantId}"]));
        Assert.Empty(rig.Store.InventoryAllocations);
    }

    [Fact]
    public async Task RepeatedCheckout_ReturnsOriginalOrderAfterReservationExhaustsStock()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 2m);
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((variantId, 2m));
        var first = await CheckoutAsync(rig, created);
        rig.Project();
        Assert.Contains("OutOfStock", ProjectedVariant(rig, variantId).Buyability.Reasons);

        var repeat = await CheckoutAsync(rig, created);
        var withoutToken = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, repeat.Status);
        Assert.Equal(first.Order!.Id, repeat.Order!.Id);
        Assert.Equal(StorefrontContextResolutionStatus.Unauthorized, withoutToken.Status);
        Assert.Equal(2m, balance.ReservedQuantity);
        Assert.Single(rig.Store.InventoryAllocations);
    }

    [Fact]
    public async Task ConcurrentCheckouts_InMemory_SellOnlyAvailableUnits()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 3m);
        rig.Project();
        var carts = new List<StorefrontCartResult>();
        for (var i = 0; i < 8; i++)
        {
            carts.Add(await rig.CreateStorefrontCartAsync((variantId, 1m)));
        }

        var results = await Task.WhenAll(carts.Select(cart => Task.Run(() => CheckoutAsync(rig, cart))));

        Assert.Equal(3, results.Count(x => x.Status == StorefrontContextResolutionStatus.Success));
        Assert.Equal(5, results.Count(x => x.Status == StorefrontContextResolutionStatus.ValidationFailed));
        Assert.Equal((3m, 3m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal(3, rig.Store.InventoryAllocations.Count);
    }

    [Fact]
    public async Task ConcurrentRepeatCheckouts_InMemory_CreateOneOrderAndOneReservation()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 10m);
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((variantId, 4m));

        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => CheckoutAsync(rig, created))));

        Assert.All(results, x => Assert.Equal(StorefrontContextResolutionStatus.Success, x.Status));
        Assert.Single(results.Select(x => x.Order!.Id).Distinct());
        Assert.Single(rig.Store.Orders.Values, x => x.SourceCartId == created.Cart!.Id);
        Assert.Equal(4m, balance.ReservedQuantity);
        Assert.Single(rig.Store.InventoryAllocations);
    }

    [Fact]
    public async Task Cancellation_ReleasesOutstandingAllocationsExactlyOnce()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 5m, reserved: 1m);
        var order = await rig.Orders.CreateAsync(ManualOrder(variantId, 3m), "tester", CancellationToken.None);
        var outboxBefore = rig.Store.OutboxMessages.Keys.ToHashSet();
        var placedRowVersion = rig.Store.Orders[order.Id].RowVersion;

        await ChangeStatusAsync(rig, order.Id, "Cancelled");

        Assert.Equal((5m, 1m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal("Released", Assert.Single(rig.Store.InventoryAllocations.Values).Status);
        var release = Assert.Single(rig.Store.InventoryTransactions.Values, x => x.Type == InventoryTransactionTypes.ReservationRelease);
        Assert.Equal((0m, -3m, order.Id), (release.QuantityDelta, release.ReservedQuantityDelta, release.ReferenceId));
        var refresh = Assert.Single(RefreshPayloads(rig, outboxBefore));
        Assert.Equal(("OrderInventoryReleased", variantId), (refresh.Reason, Assert.Single(refresh.VariantIds)));

        await Assert.ThrowsAsync<RequestValidationException>(() => ChangeStatusAsync(rig, order.Id, "Cancelled"));
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            rig.Orders.ChangeStatusAsync(new ChangeOrderStatusCommand(order.Id, "Cancelled", null, placedRowVersion), "tester", CancellationToken.None));
        Assert.Equal(1m, balance.ReservedQuantity);
        Assert.Single(rig.Store.InventoryTransactions.Values, x => x.Type == InventoryTransactionTypes.ReservationRelease);
    }

    [Fact]
    public async Task Completion_ShipsReservedStockOnceWithoutChangingAvailability()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 5m);
        var order = await rig.Orders.CreateAsync(ManualOrder(variantId, 2.5m), "tester", CancellationToken.None);
        await ChangeStatusAsync(rig, order.Id, "Processing");
        var availableBefore = balance.AvailableQuantity;
        var outboxBefore = rig.Store.OutboxMessages.Count;

        await ChangeStatusAsync(rig, order.Id, "Completed");

        Assert.Equal((2.5m, 0m, availableBefore), (balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity));
        Assert.Equal("Shipped", Assert.Single(rig.Store.InventoryAllocations.Values).Status);
        var shipment = Assert.Single(rig.Store.InventoryTransactions.Values, x => x.Type == InventoryTransactionTypes.Shipment);
        Assert.Equal((-2.5m, -2.5m), (shipment.QuantityDelta, shipment.ReservedQuantityDelta));
        Assert.Equal(outboxBefore, rig.Store.OutboxMessages.Count);
        await Assert.ThrowsAsync<RequestValidationException>(() => ChangeStatusAsync(rig, order.Id, "Cancelled"));
        Assert.Equal((2.5m, 0m), (balance.OnHandQuantity, balance.ReservedQuantity));
    }

    [Fact]
    public async Task Completion_IsRejectedWhenNonBackorderableStockWasCountedAway()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 5m);
        var order = await rig.Orders.CreateAsync(ManualOrder(variantId, 3m), "tester", CancellationToken.None);
        await ChangeStatusAsync(rig, order.Id, "Processing");
        await rig.Inventory.AdjustInventoryAsync(
            new AdjustInventoryCommand(VatPricingTestRig.MainLocationId, variantId, "Adjustment", -4m, "Shrinkage", null),
            CancellationToken.None);
        var rowVersion = rig.Store.Orders[order.Id].RowVersion;
        var snapshot = Snapshot(rig);

        await Assert.ThrowsAsync<RequestValidationException>(() => ChangeStatusAsync(rig, order.Id, "Completed"));

        Assert.Equal(("Processing", rowVersion), (rig.Store.Orders[order.Id].Status, rig.Store.Orders[order.Id].RowVersion));
        Assert.Equal(snapshot, Snapshot(rig));

        // Cancelling instead releases the reservation even though on-hand stock is short.
        await ChangeStatusAsync(rig, order.Id, "Cancelled");
        Assert.Equal((1m, 0m), (balance.OnHandQuantity, balance.ReservedQuantity));
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public async Task LegacyOrderWithoutAllocations_SettlesWithoutInventoryChanges(string toStatus)
    {
        var rig = new VatPricingTestRig();
        var balance = rig.Store.InventoryBalances[SeededBalanceId];
        var snapshot = Snapshot(rig);
        var outboxBefore = rig.Store.OutboxMessages.Count;

        await ChangeStatusAsync(rig, SeededOrderId, toStatus);

        Assert.Equal(toStatus, rig.Store.Orders[SeededOrderId].Status);
        Assert.Equal((25m, 2m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal(snapshot, Snapshot(rig));
        Assert.Equal(outboxBefore, rig.Store.OutboxMessages.Count);
    }

    [Fact]
    public async Task AdminCartConversionAndManualOrder_ReserveThroughTheSharedPath()
    {
        var rig = new VatPricingTestRig();
        var balance = rig.Store.InventoryBalances[SeededBalanceId];
        var cart = rig.Store.Carts[SeededCartId];

        var fromCart = await rig.Orders.CreateAsync(
            new CreateOrderCommand(cart.Id, cart.RowVersion, null, null, null, null, null, null, [], []),
            "tester",
            CancellationToken.None);
        var manual = await rig.Orders.CreateAsync(ManualOrder(VatPricingTestRig.SeededVariantId, 1m), "tester", CancellationToken.None);

        // The seed's 2 reserved units are an external hold with no allocation; orders add on top.
        Assert.Equal((25m, 5m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal(2m, rig.Store.InventoryAllocations.Values.Single(x => x.OrderId == fromCart.Id).Quantity);
        Assert.Equal(1m, rig.Store.InventoryAllocations.Values.Single(x => x.OrderId == manual.Id).Quantity);
        Assert.Equal(0.25m, Assert.Single(fromCart.Lines).VatRate);
        Assert.Equal(1499.00m, manual.GrandTotal);
    }

    [Fact]
    public async Task AdminSnapshot_CannotDropReservedBelowOpenAllocations()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 10m);
        await rig.Orders.CreateAsync(ManualOrder(variantId, 3m), "tester", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            rig.Inventory.UpsertBalanceAsync(Upsert(variantId, onHand: 10m, reserved: 1m, balance.RowVersion), CancellationToken.None));
        Assert.Contains("ReservedQuantity", exception.Errors.Keys);
        Assert.Equal(3m, balance.ReservedQuantity);

        var withExternalHold = await rig.Inventory.UpsertBalanceAsync(Upsert(variantId, onHand: 12m, reserved: 4m, balance.RowVersion), CancellationToken.None);
        Assert.Equal((12m, 4m), (withExternalHold.OnHandQuantity, withExternalHold.ReservedQuantity));
    }

    [Fact]
    public async Task AdminAdjustment_RejectedBeforeMutatingTheBalance()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        var rowVersion = balance.RowVersion;

        await Assert.ThrowsAsync<RequestValidationException>(() => rig.Inventory.AdjustInventoryAsync(
            new AdjustInventoryCommand(VatPricingTestRig.MainLocationId, variantId, "Adjustment", -1001m, "Recount", null),
            CancellationToken.None));

        Assert.Equal((1000m, rowVersion), (balance.OnHandQuantity, balance.RowVersion));
    }

    [Fact]
    public async Task ConcurrentAdminAdjustmentsAndCheckouts_InMemory_LoseNoUpdates()
    {
        var rig = new VatPricingTestRig();
        var variantId = rig.AddProduct(1, "STANDARD", 100m);
        var balance = MainBalance(rig, variantId);
        SetStock(balance, onHand: 4m);
        rig.Project();
        var carts = new List<StorefrontCartResult>();
        for (var i = 0; i < 10; i++)
        {
            carts.Add(await rig.CreateStorefrontCartAsync((variantId, 1m)));
        }

        var checkouts = carts.Select(cart => Task.Run(() => CheckoutAsync(rig, cart))).ToList();
        var adjustments = Enumerable.Range(0, 10).Select(_ => Task.Run(() => rig.Inventory.AdjustInventoryAsync(
            new AdjustInventoryCommand(VatPricingTestRig.MainLocationId, variantId, "Adjustment", 1m, "Receipt", null),
            CancellationToken.None))).ToList();
        await Task.WhenAll(adjustments);
        var results = await Task.WhenAll(checkouts);

        var sold = results.Count(x => x.Status == StorefrontContextResolutionStatus.Success);
        Assert.Equal(14m, balance.OnHandQuantity);
        Assert.Equal(sold, balance.ReservedQuantity);
        Assert.InRange(sold, 4, 10);
        Assert.True(balance.AvailableQuantity >= 0m);
    }

    private static Task<StorefrontCheckoutResult> CheckoutAsync(VatPricingTestRig rig, StorefrontCartResult created)
    {
        return rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);
    }

    private static async Task<string> ChangeStatusAsync(VatPricingTestRig rig, Guid orderId, string toStatus)
    {
        var order = rig.Store.Orders[orderId];
        await rig.Orders.ChangeStatusAsync(new ChangeOrderStatusCommand(orderId, toStatus, null, order.RowVersion), "tester", CancellationToken.None);
        return order.RowVersion;
    }

    private static CreateOrderCommand ManualOrder(Guid variantId, decimal quantity, Guid? marketId = null, string currencyCode = "SEK")
    {
        return new CreateOrderCommand(
            null,
            null,
            null,
            null,
            marketId ?? VatPricingTestRig.SeMarketId,
            currencyCode,
            "sv-SE",
            "buyer@example.com",
            [new CreateOrderLineItem(variantId, quantity, null)],
            []);
    }

    private static UpsertInventoryBalanceCommand Upsert(Guid variantId, decimal onHand, decimal reserved, string rowVersion)
    {
        return new UpsertInventoryBalanceCommand(VatPricingTestRig.MainLocationId, variantId, onHand, reserved, 0m, false, rowVersion);
    }

    private static InventoryBalance MainBalance(VatPricingTestRig rig, Guid variantId)
    {
        return rig.Store.InventoryBalances.Values.Single(x => x.VariantId == variantId && x.InventoryLocationId == VatPricingTestRig.MainLocationId);
    }

    private static void SetStock(InventoryBalance balance, decimal onHand, decimal reserved = 0m, bool backorderable = false)
    {
        balance.UpdateSnapshot(onHand, reserved, 0m, backorderable, balance.RowVersion);
    }

    private static string Snapshot(VatPricingTestRig rig)
    {
        var balances = rig.Store.InventoryBalances.Values
            .OrderBy(x => x.Id)
            .Select(x => $"{x.Id}:{x.OnHandQuantity}:{x.ReservedQuantity}:{x.RowVersion}");
        return string.Join("|", balances)
            + $"|orders={rig.Store.Orders.Count}|allocations={string.Join(",", rig.Store.InventoryAllocations.Values.OrderBy(x => x.Id).Select(x => $"{x.Id}:{x.Status}"))}"
            + $"|transactions={rig.Store.InventoryTransactions.Count}|outbox={rig.Store.OutboxMessages.Count}";
    }

    private static IEnumerable<StorefrontProjectionRefreshRequestedPayload> RefreshPayloads(VatPricingTestRig rig, IReadOnlySet<Guid> existingMessageIds)
    {
        return rig.Store.OutboxMessages.Values
            .Where(x => !existingMessageIds.Contains(x.Id))
            .Where(x => x.EventType == WebhookEventTypes.StorefrontProjectionRefreshRequested)
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => JsonSerializer.Deserialize<StorefrontProjectionRefreshRequestedPayload>(x.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!);
    }

    private static StorefrontProductVariantDto ProjectedVariant(VatPricingTestRig rig, Guid variantId)
    {
        return rig.Store.StorefrontProductProjections.Values
            .Where(x => x.MarketCode == "SE")
            .SelectMany(x => JsonSerializer.Deserialize<List<StorefrontProductVariantDto>>(x.VariantsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!)
            .First(x => x.Id == variantId);
    }
}
