using Microsoft.EntityFrameworkCore;
using Platform.Application.Catalog.Channels;
using Platform.Application.Catalog.Channels.Queries;
using Platform.Domain.Catalog.Channels;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Catalog.Channels;

public sealed class EfChannelRepository : IChannelRepository
{
    private readonly PlatformDbContext _dbContext;

    public EfChannelRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChannelListResult> ListAsync(ListChannelsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filteredQuery = _dbContext.Channels
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(query.Search)
                || x.Code.Contains(query.Search)
                || x.Name.Contains(query.Search)
                || (x.HostName != null && x.HostName.Contains(query.Search)))
            .Where(x => string.IsNullOrWhiteSpace(query.Status) || x.Status == query.Status);

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(filteredQuery, query.Sort)
            .Include(x => x.MarketAssignments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ChannelListResult(items, total);
    }

    public async Task<Channel?> GetByIdAsync(Guid channelId, CancellationToken cancellationToken)
    {
        return await _dbContext.Channels
            .Include(x => x.MarketAssignments)
            .FirstOrDefaultAsync(x => x.Id == channelId, cancellationToken);
    }

    public async Task<IReadOnlyList<Channel>> GetByIdsAsync(IReadOnlyCollection<Guid> channelIds, CancellationToken cancellationToken)
    {
        if (channelIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Channels
            .AsNoTracking()
            .Include(x => x.MarketAssignments)
            .Where(x => channelIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Channel?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Channels
            .Include(x => x.MarketAssignments)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<Channel?> GetByHostNameAsync(string hostName, CancellationToken cancellationToken)
    {
        return await _dbContext.Channels
            .Include(x => x.MarketAssignments)
            .FirstOrDefaultAsync(x => x.HostName == hostName, cancellationToken);
    }

    public async Task AddAsync(Channel channel, CancellationToken cancellationToken)
    {
        await _dbContext.Channels.AddAsync(channel, cancellationToken);
    }

    private static IQueryable<Channel> ApplySorting(IQueryable<Channel> channels, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "-updatedatutc" => channels.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "updatedatutc" => channels.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.Code),
            "-name" => channels.OrderByDescending(x => x.Name).ThenBy(x => x.Code),
            "name" => channels.OrderBy(x => x.Name).ThenBy(x => x.Code),
            "-code" => channels.OrderByDescending(x => x.Code),
            _ => channels.OrderBy(x => x.Code)
        };
    }
}
