using Platform.Application.Abstractions.Persistence;

namespace Platform.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly PlatformDbContext _dbContext;

    public EfUnitOfWork(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
