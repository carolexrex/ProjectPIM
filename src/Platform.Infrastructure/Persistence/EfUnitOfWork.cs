using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Domain.Catalog.Inventory;
using Platform.Domain.Catalog.Tax;
using Platform.Domain.Common;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence.Configurations.Catalog;
using Platform.Infrastructure.Persistence.Configurations.Integrations;
using Platform.Infrastructure.Persistence.Configurations.Order;
using CartEntity = Platform.Domain.Cart.Cart;
using OrderEntity = Platform.Domain.Orders.Order;

namespace Platform.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private const string UniqueViolationSqlState = "23505";
    private const string SerializationFailureSqlState = "40001";
    private const string DeadlockDetectedSqlState = "40P01";

    private readonly PlatformDbContext _dbContext;

    public EfUnitOfWork(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsOutboxDedupKeyViolation(exception))
        {
            // PostgreSQL-specific: the partial unique index on unpublished outbox dedup keys is the
            // authority when several producers schedule the same work at once. Surfacing it as a
            // domain exception keeps callers provider-neutral and matches the in-memory store.
            throw new DuplicateOutboxDedupKeyException(
                ResolveDedupKey() ?? OutboxMessageConfiguration.ReservedDedupKeyIndexName,
                exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, VatRuleConfiguration.MarketTaxCategoryIndexName))
        {
            // PostgreSQL-specific: two concurrent creates for the same market and tax category both
            // pass the application-level check; the unique index decides and the loser gets a conflict.
            throw new ConflictException("A VAT rule already exists for this market and tax category.");
        }
        catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(x => x.Entity is VatRule))
        {
            // The VAT rule row version changed between load and save (concurrent update or delete).
            throw new ConcurrencyException("The VAT rule has changed since it was loaded.");
        }
        catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(x => IsCommerceEntity(x.Entity)))
        {
            // A balance, allocation, cart, or order row version changed between load and save: another
            // checkout, settlement, or admin stock write committed first. The whole save rolled back.
            throw new ConcurrentWriteException(
                "Inventory, cart, or order data changed during this operation. Retry the request.",
                exception);
        }
        catch (DbUpdateException exception) when (
            IsUniqueViolation(exception, OrderConfiguration.SourceCartIndexName)
            || IsUniqueViolation(exception, OrderConfiguration.OrderNumberIndexName)
            || IsUniqueViolation(exception, InventoryAllocationConfiguration.OrderBalanceIndexName))
        {
            // PostgreSQL-specific: a concurrent checkout of the same cart (or an order-number collision)
            // inserted first. Retrying lets the caller observe the committed order instead of failing.
            throw new ConcurrentWriteException("A concurrent operation created the same order first. Retry the request.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: SerializationFailureSqlState or DeadlockDetectedSqlState })
        {
            // PostgreSQL-specific: the server aborted this transaction to resolve a lock conflict with a
            // concurrent one. Nothing was committed, so the whole operation is safe to rerun.
            throw new ConcurrentWriteException("The database aborted a conflicting concurrent write. Retry the request.", exception);
        }
    }

    private static bool IsCommerceEntity(object entity)
    {
        return entity is InventoryBalance or InventoryAllocation or CartEntity or OrderEntity;
    }

    private static bool IsOutboxDedupKeyViolation(DbUpdateException exception)
    {
        return IsUniqueViolation(exception, OutboxMessageConfiguration.ReservedDedupKeyIndexName);
    }

    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName)
    {
        return exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState } postgresException
            && string.Equals(postgresException.ConstraintName, constraintName, StringComparison.Ordinal);
    }

    private string? ResolveDedupKey()
    {
        return _dbContext.ChangeTracker.Entries<OutboxMessage>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity.DedupKey)
            .FirstOrDefault(x => x is not null);
    }
}
