using Microsoft.EntityFrameworkCore;
using Platform.Application.Storefront;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Storefront;

public sealed class EfStorefrontProjectionChangeTracker : IStorefrontProjectionChangeTracker
{
    private readonly PlatformDbContext _dbContext;

    public EfStorefrontProjectionChangeTracker(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void DiscardPendingChanges()
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<StorefrontProductProjection>().ToList())
        {
            entry.State = entry.State == EntityState.Added
                ? EntityState.Detached
                : EntityState.Unchanged;
        }
    }
}
