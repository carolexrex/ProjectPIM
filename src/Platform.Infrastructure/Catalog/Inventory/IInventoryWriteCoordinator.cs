namespace Platform.Infrastructure.Catalog.Inventory;

/// <summary>
/// Runs a write that changes inventory reservations or stock (order placement, order settlement,
/// admin balance writes) with the provider's concurrency guarantees.
/// </summary>
/// <remarks>
/// PostgreSQL: balances, allocations, carts, and orders carry row-version concurrency tokens and the
/// order source-cart key is unique, so the database rejects a conflicting save. The coordinator then
/// discards the tracked changes and reruns the whole operation against fresh state, a bounded number
/// of times. A rerun re-plans from current stock, so a lost race surfaces as a clear stock error and a
/// lost duplicate checkout finds the winning order.
/// In-memory: there are no transactions or tokens, so operations run one at a time under a store-wide
/// lock and must validate everything before their first mutation.
/// </remarks>
public interface IInventoryWriteCoordinator
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
