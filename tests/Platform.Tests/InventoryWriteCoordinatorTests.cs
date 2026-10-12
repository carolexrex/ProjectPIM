using Microsoft.EntityFrameworkCore;
using Platform.Application.Abstractions.Errors;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Common;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Security;

namespace Platform.Tests;

public sealed class InventoryWriteCoordinatorTests
{
    [Fact]
    public async Task EfUnitOfWork_MapsStaleBalanceSaveToConcurrentWriteException()
    {
        var databaseName = $"coordinator-{Guid.NewGuid():N}";
        var balance = new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5m, 0m, 0m, false, DateTime.UtcNow);
        await using (var setup = CreateContext(databaseName))
        {
            setup.InventoryBalances.Add(balance);
            await setup.SaveChangesAsync();
        }

        await using var first = CreateContext(databaseName);
        await using var second = CreateContext(databaseName);
        var firstBalance = await first.InventoryBalances.SingleAsync();
        var secondBalance = await second.InventoryBalances.SingleAsync();
        firstBalance.Reserve(2m, Guid.NewGuid(), DateTime.UtcNow);
        await new EfUnitOfWork(first).SaveChangesAsync(CancellationToken.None);

        secondBalance.Reserve(4m, Guid.NewGuid(), DateTime.UtcNow);
        await Assert.ThrowsAsync<ConcurrentWriteException>(() => new EfUnitOfWork(second).SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task EfCoordinator_RerunsOnConcurrentWriteWithAFreshChangeTracker()
    {
        await using var db = CreateContext($"coordinator-{Guid.NewGuid():N}");
        var coordinator = new EfInventoryWriteCoordinator(db);
        var attempts = 0;

        var result = await coordinator.ExecuteAsync(_ =>
        {
            attempts++;
            Assert.Empty(db.ChangeTracker.Entries());
            db.InventoryBalances.Add(new InventoryBalance(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1m, 0m, 0m, false, DateTime.UtcNow));
            return attempts < 3 ? throw new ConcurrentWriteException("lost race") : Task.FromResult(attempts);
        }, CancellationToken.None);

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task EfCoordinator_GivesUpAfterMaxAttemptsAndDoesNotRetryOtherFailures()
    {
        await using var db = CreateContext($"coordinator-{Guid.NewGuid():N}");
        var coordinator = new EfInventoryWriteCoordinator(db);
        var conflictAttempts = 0;
        var staleAttempts = 0;

        await Assert.ThrowsAsync<ConcurrentWriteException>(() => coordinator.ExecuteAsync<int>(_ =>
        {
            conflictAttempts++;
            throw new ConcurrentWriteException("lost race");
        }, CancellationToken.None));
        await Assert.ThrowsAsync<ConcurrencyException>(() => coordinator.ExecuteAsync<int>(_ =>
        {
            staleAttempts++;
            throw new ConcurrencyException("client sent a stale row version");
        }, CancellationToken.None));

        Assert.Equal(EfInventoryWriteCoordinator.MaxAttempts, conflictAttempts);
        Assert.Equal(1, staleAttempts);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task InMemoryCoordinator_RunsOperationsOneAtATime()
    {
        var coordinator = new InMemoryInventoryWriteCoordinator(new InMemoryCatalogStore());
        var active = 0;
        var maxActive = 0;

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => coordinator.ExecuteAsync(async _ =>
        {
            var now = Interlocked.Increment(ref active);
            maxActive = Math.Max(maxActive, now);
            await Task.Delay(1);
            Interlocked.Decrement(ref active);
            return now;
        }, CancellationToken.None))));

        Assert.Equal(1, maxActive);
    }

    private static PlatformDbContext CreateContext(string databaseName)
    {
        return new PlatformDbContext(
            new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(databaseName).Options,
            new SystemCurrentActorAccessor());
    }
}
