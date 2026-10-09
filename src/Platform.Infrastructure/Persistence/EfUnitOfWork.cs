using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Application.Abstractions.Persistence;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence.Configurations.Integrations;

namespace Platform.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private const string UniqueViolationSqlState = "23505";

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
    }

    private static bool IsOutboxDedupKeyViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: UniqueViolationSqlState,
            ConstraintName: OutboxMessageConfiguration.ReservedDedupKeyIndexName
        };
    }

    private string? ResolveDedupKey()
    {
        return _dbContext.ChangeTracker.Entries<OutboxMessage>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity.DedupKey)
            .FirstOrDefault(x => x is not null);
    }
}
