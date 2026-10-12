using Platform.Application.Abstractions.Errors;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class EfInventoryWriteCoordinator : IInventoryWriteCoordinator
{
    public const int MaxAttempts = 3;

    private readonly PlatformDbContext _dbContext;

    public EfInventoryWriteCoordinator(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (ConcurrentWriteException)
            {
                // The failed SaveChanges rolled back its transaction. Drop everything this context
                // tracked so the next attempt reloads current rows and nothing stale can be re-sent.
                _dbContext.ChangeTracker.Clear();
                if (attempt >= MaxAttempts)
                {
                    throw;
                }
            }
        }
    }
}
