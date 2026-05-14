using Microsoft.EntityFrameworkCore;
using Platform.Application.Integrations;
using Platform.Domain.Integrations;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Integrations;

public sealed class EfOutboxMessageRepository : IOutboxMessageRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfOutboxMessageRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OutboxMessage?> GetNextUnpublishedAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.OutboxMessages
            .Where(x => !x.PublishedAtUtc.HasValue)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
