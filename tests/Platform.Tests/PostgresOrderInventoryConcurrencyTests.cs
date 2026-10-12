using Microsoft.EntityFrameworkCore;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Orders.Commands;
using Platform.Domain.Common;
using Platform.Infrastructure.Persistence;

namespace Platform.Tests;

/// <summary>
/// Order inventory concurrency against real PostgreSQL. The interleaving tests commit a competing
/// operation between an operation's reads and its save, so the row-version token must reject the
/// stale save and the inventory write coordinator must rerun against fresh state. The race tests run
/// operations in parallel on separate connections.
/// </summary>
public sealed class PostgresOrderInventoryConcurrencyTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public PostgresOrderInventoryConcurrencyTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    [PostgresFact]
    public async Task LosingCheckoutForLastUnit_RollsBackRetriesAndReportsInsufficientStock()
    {
        var harness = await CreateHarnessAsync();
        var (variantId, balanceId) = await harness.SeedVariantAsync("LAST-1", onHand: 1m);
        var winnerCart = await harness.SeedCartAsync((variantId, 1m));
        var loserCart = await harness.SeedCartAsync((variantId, 1m));

        await using var loserDb = harness.CreateContext();
        var interleaving = new InterleavingUnitOfWork(new EfUnitOfWork(loserDb), async () =>
        {
            await using var winnerDb = harness.CreateContext();
            await EfCommerceTestHarness.CreateOrders(winnerDb).CreateAsync(FromCart(winnerCart), "winner", CancellationToken.None);
        });

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            EfCommerceTestHarness.CreateOrders(loserDb, interleaving).CreateAsync(FromCart(loserCart), "loser", CancellationToken.None));

        // One save was attempted and rejected; the rerun stopped at planning with a stock error.
        Assert.Equal(1, interleaving.SaveAttempts);
        Assert.Contains("requested 1, available 0", Assert.Single(exception.Errors[$"VariantId.{variantId}"]));
        await using var verify = harness.CreateContext();
        var balance = await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == balanceId);
        Assert.Equal((1m, 1m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Single(await verify.Orders.AsNoTracking().ToListAsync());
        Assert.Single(await verify.InventoryAllocations.AsNoTracking().ToListAsync());
        Assert.Single(await verify.InventoryTransactions.AsNoTracking().Where(x => x.Type == "Reservation").ToListAsync());
        Assert.Equal(1, await verify.OutboxMessages.CountAsync());
        var cart = await verify.Carts.AsNoTracking().SingleAsync(x => x.Id == loserCart.CartId);
        Assert.Equal(("Active", loserCart.RowVersion), (cart.Status, cart.RowVersion));
    }

    [PostgresFact]
    public async Task LosingRepeatCheckoutOfSameCart_ReturnsTheWinningOrder()
    {
        var harness = await CreateHarnessAsync();
        var (variantId, balanceId) = await harness.SeedVariantAsync("REPEAT-1", onHand: 5m);
        var cart = await harness.SeedCartAsync((variantId, 2m), (variantId, 1m));
        Guid winnerOrderId = default;

        await using var loserDb = harness.CreateContext();
        var interleaving = new InterleavingUnitOfWork(new EfUnitOfWork(loserDb), async () =>
        {
            await using var winnerDb = harness.CreateContext();
            winnerOrderId = (await EfCommerceTestHarness.CreateOrders(winnerDb).CreateAsync(FromCart(cart), "winner", CancellationToken.None)).Id;
        });

        var repeat = await EfCommerceTestHarness.CreateOrders(loserDb, interleaving).CreateAsync(FromCart(cart), "loser", CancellationToken.None);

        Assert.Equal(winnerOrderId, repeat.Id);
        await using var verify = harness.CreateContext();
        Assert.Single(await verify.Orders.AsNoTracking().ToListAsync());
        var allocation = Assert.Single(await verify.InventoryAllocations.AsNoTracking().ToListAsync());
        Assert.Equal(3m, allocation.Quantity);
        Assert.Equal(3m, (await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == balanceId)).ReservedQuantity);
    }

    [PostgresFact]
    public async Task AdminAdjustmentRacingCheckout_RetriesWithoutLosingTheReservation()
    {
        var harness = await CreateHarnessAsync();
        var (variantId, balanceId) = await harness.SeedVariantAsync("ADJUST-1", onHand: 4m);
        var cart = await harness.SeedCartAsync((variantId, 3m));

        await using var adminDb = harness.CreateContext();
        var interleaving = new InterleavingUnitOfWork(new EfUnitOfWork(adminDb), async () =>
        {
            await using var checkoutDb = harness.CreateContext();
            await EfCommerceTestHarness.CreateOrders(checkoutDb).CreateAsync(FromCart(cart), "checkout", CancellationToken.None);
        });

        await EfCommerceTestHarness.CreateInventory(adminDb, interleaving).AdjustInventoryAsync(
            new AdjustInventoryCommand(harness.LocationId, variantId, "Adjustment", 6m, "Receipt", null),
            CancellationToken.None);

        Assert.Equal(2, interleaving.SaveAttempts);
        await using var verify = harness.CreateContext();
        var balance = await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == balanceId);
        Assert.Equal((10m, 3m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Equal(2, await verify.InventoryTransactions.CountAsync(x => x.VariantId == variantId));
    }

    [PostgresFact]
    public async Task CheckoutRacingStaleAdminSnapshot_RejectsTheSnapshotWithConflict()
    {
        var harness = await CreateHarnessAsync();
        var (variantId, balanceId) = await harness.SeedVariantAsync("SNAPSHOT-1", onHand: 4m);
        var cart = await harness.SeedCartAsync((variantId, 3m));

        // The snapshot carries no row version, so only the database token can catch the race.
        await using var adminDb = harness.CreateContext();
        var interleaving = new InterleavingUnitOfWork(new EfUnitOfWork(adminDb), async () =>
        {
            await using var checkoutDb = harness.CreateContext();
            await EfCommerceTestHarness.CreateOrders(checkoutDb).CreateAsync(FromCart(cart), "checkout", CancellationToken.None);
        });

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            EfCommerceTestHarness.CreateInventory(adminDb, interleaving).UpsertBalanceAsync(
                new UpsertInventoryBalanceCommand(harness.LocationId, variantId, 4m, 0m, 0m, false, null),
                CancellationToken.None));

        // The rerun saw the new reservation and refused to drop reserved stock below it.
        Assert.Contains("ReservedQuantity", exception.Errors.Keys);
        await using var verify = harness.CreateContext();
        Assert.Equal(3m, (await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == balanceId)).ReservedQuantity);
    }

    [PostgresFact]
    public async Task ConcurrentCancellation_ReleasesOnceAndRejectsTheStaleCancel()
    {
        var harness = await CreateHarnessAsync();
        var (variantId, balanceId) = await harness.SeedVariantAsync("CANCEL-1", onHand: 4m);
        var cart = await harness.SeedCartAsync((variantId, 3m));
        Guid orderId;
        string rowVersion;
        await using (var placeDb = harness.CreateContext())
        {
            var order = await EfCommerceTestHarness.CreateOrders(placeDb).CreateAsync(FromCart(cart), "checkout", CancellationToken.None);
            (orderId, rowVersion) = (order.Id, order.RowVersion);
        }

        await using var loserDb = harness.CreateContext();
        var interleaving = new InterleavingUnitOfWork(new EfUnitOfWork(loserDb), async () =>
        {
            await using var winnerDb = harness.CreateContext();
            await EfCommerceTestHarness.CreateOrders(winnerDb).ChangeStatusAsync(new ChangeOrderStatusCommand(orderId, "Cancelled", null, rowVersion), "winner", CancellationToken.None);
        });

        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            EfCommerceTestHarness.CreateOrders(loserDb, interleaving).ChangeStatusAsync(new ChangeOrderStatusCommand(orderId, "Cancelled", null, rowVersion), "loser", CancellationToken.None));

        await using var verify = harness.CreateContext();
        Assert.Equal(0m, (await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == balanceId)).ReservedQuantity);
        Assert.Equal(1, await verify.InventoryTransactions.CountAsync(x => x.Type == "ReservationRelease"));
        Assert.Equal("Released", (await verify.InventoryAllocations.AsNoTracking().SingleAsync()).Status);
    }

    [PostgresFact]
    public async Task ParallelCheckoutsForLastUnit_SellExactlyOne()
    {
        var harness = await CreateHarnessAsync();
        var (variantId, balanceId) = await harness.SeedVariantAsync("RACE-1", onHand: 1m);
        var carts = new List<(Guid CartId, string RowVersion)>();
        for (var i = 0; i < 12; i++)
        {
            carts.Add(await harness.SeedCartAsync((variantId, 1m)));
        }

        var outcomes = await RunInParallelAsync(carts.Select<(Guid CartId, string RowVersion), Func<Task>>(cart => async () =>
        {
            await using var db = harness.CreateContext();
            await EfCommerceTestHarness.CreateOrders(db).CreateAsync(FromCart(cart), "race", CancellationToken.None);
        }));

        Assert.Equal(1, outcomes.Count(x => x is null));
        Assert.All(outcomes.Where(x => x is not null), x => Assert.True(x is RequestValidationException or ConcurrentWriteException, x!.ToString()));
        await using var verify = harness.CreateContext();
        var balance = await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == balanceId);
        Assert.Equal((1m, 1m), (balance.OnHandQuantity, balance.ReservedQuantity));
        Assert.Single(await verify.Orders.AsNoTracking().ToListAsync());
        Assert.Single(await verify.InventoryAllocations.AsNoTracking().ToListAsync());
        Assert.Equal(11, await verify.Carts.CountAsync(x => x.Status == "Active"));
    }

    [PostgresFact]
    public async Task ParallelMultiLineCheckoutsAndAdminAdjustments_NeverOversell()
    {
        var harness = await CreateHarnessAsync();
        var (scarceId, scarceBalanceId) = await harness.SeedVariantAsync("RACE-SCARCE", onHand: 5m);
        var (plentyId, plentyBalanceId) = await harness.SeedVariantAsync("RACE-PLENTY", onHand: 1000m);
        var carts = new List<(Guid CartId, string RowVersion)>();
        for (var i = 0; i < 10; i++)
        {
            // Duplicate fractional lines: each order needs 2.5 of the scarce variant.
            carts.Add(await harness.SeedCartAsync((plentyId, 2m), (scarceId, 1.5m), (scarceId, 1m)));
        }

        var checkouts = carts.Select<(Guid CartId, string RowVersion), Func<Task>>(cart => async () =>
        {
            await using var db = harness.CreateContext();
            await EfCommerceTestHarness.CreateOrders(db).CreateAsync(FromCart(cart), "race", CancellationToken.None);
        });
        var adjustments = Enumerable.Range(0, 5).Select<int, Func<Task>>(_ => async () =>
        {
            await using var db = harness.CreateContext();
            await EfCommerceTestHarness.CreateInventory(db).AdjustInventoryAsync(
                new AdjustInventoryCommand(harness.LocationId, scarceId, "Adjustment", 0.5m, "Receipt", null),
                CancellationToken.None);
        });
        var outcomes = await RunInParallelAsync(checkouts.Concat(adjustments));

        var unexpected = outcomes.Where(x => x is not null and not RequestValidationException and not ConcurrentWriteException).ToList();
        Assert.Empty(unexpected);
        Assert.All(
            outcomes.OfType<RequestValidationException>(),
            x => Assert.Contains("Insufficient stock for RACE-SCARCE", Assert.Single(x.Errors.Values.SelectMany(messages => messages))));
        await using var verify = harness.CreateContext();
        var orders = await verify.Orders.AsNoTracking().CountAsync();
        var adjusted = await verify.InventoryTransactions.CountAsync(x => x.Type == "Adjustment");
        var scarce = await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == scarceBalanceId);
        var plenty = await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == plentyBalanceId);
        Assert.InRange(orders, 1, 3);
        Assert.Equal(5m + 0.5m * adjusted, scarce.OnHandQuantity);
        Assert.Equal(2.5m * orders, scarce.ReservedQuantity);
        Assert.True(scarce.AvailableQuantity >= 0m);
        Assert.Equal(2m * orders, plenty.ReservedQuantity);
        Assert.Equal(2 * orders, await verify.InventoryAllocations.CountAsync());
        Assert.Equal(orders, await verify.InventoryAllocations.CountAsync(x => x.VariantId == scarceId && x.Quantity == 2.5m));
    }

    [PostgresFact]
    public async Task ParallelRepeatCheckoutsOfOneCart_ReturnOneOrder()
    {
        var harness = await CreateHarnessAsync();
        var (variantId, balanceId) = await harness.SeedVariantAsync("RACE-REPEAT", onHand: 10m);
        var cart = await harness.SeedCartAsync((variantId, 4m));
        var orderIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();

        var outcomes = await RunInParallelAsync(Enumerable.Range(0, 8).Select<int, Func<Task>>(_ => async () =>
        {
            await using var db = harness.CreateContext();
            orderIds.Add((await EfCommerceTestHarness.CreateOrders(db).CreateAsync(FromCart(cart), "race", CancellationToken.None)).Id);
        }));

        Assert.All(outcomes, x => Assert.Null(x));
        Assert.Single(orderIds.Distinct());
        await using var verify = harness.CreateContext();
        Assert.Single(await verify.Orders.AsNoTracking().ToListAsync());
        Assert.Equal(4m, (await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == balanceId)).ReservedQuantity);
        Assert.Single(await verify.InventoryAllocations.AsNoTracking().ToListAsync());
    }

    [PostgresFact]
    public async Task ShortLineInMultiLineOrder_CommitsNothing()
    {
        var harness = await CreateHarnessAsync();
        var (plentyId, plentyBalanceId) = await harness.SeedVariantAsync("ROLLBACK-PLENTY", onHand: 10m);
        var (scarceId, _) = await harness.SeedVariantAsync("ROLLBACK-SCARCE", onHand: 1m);
        var cart = await harness.SeedCartAsync((plentyId, 2m), (scarceId, 2m));

        await using (var db = harness.CreateContext())
        {
            await Assert.ThrowsAsync<RequestValidationException>(() =>
                EfCommerceTestHarness.CreateOrders(db).CreateAsync(FromCart(cart), "tester", CancellationToken.None));
        }

        await using var verify = harness.CreateContext();
        Assert.Equal(0m, (await verify.InventoryBalances.AsNoTracking().SingleAsync(x => x.Id == plentyBalanceId)).ReservedQuantity);
        Assert.Empty(await verify.Orders.AsNoTracking().ToListAsync());
        Assert.Empty(await verify.InventoryAllocations.AsNoTracking().ToListAsync());
        Assert.Equal(0, await verify.OutboxMessages.CountAsync());
        Assert.Equal("Active", (await verify.Carts.AsNoTracking().SingleAsync(x => x.Id == cart.CartId)).Status);
    }

    private async Task<EfCommerceTestHarness> CreateHarnessAsync()
    {
        var harness = EfCommerceTestHarness.ForPostgres(await _database.CreateDatabaseAsync());
        await harness.SeedMarketAsync();
        return harness;
    }

    private static async Task<IReadOnlyList<Exception?>> RunInParallelAsync(IEnumerable<Func<Task>> operations)
    {
        using var start = new ManualResetEventSlim(false);
        var tasks = operations.Select(operation => Task.Run(async () =>
        {
            start.Wait();
            try
            {
                await operation();
                return (Exception?)null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        })).ToList();

        start.Set();
        return await Task.WhenAll(tasks);
    }

    private static CreateOrderCommand FromCart((Guid CartId, string RowVersion) cart)
    {
        return new CreateOrderCommand(cart.CartId, cart.RowVersion, null, null, null, null, null, null, [], []);
    }
}
