using Platform.Application.Catalog.Channels;
using Platform.Application.Catalog.Channels.Queries;
using Platform.Domain.Catalog.Channels;

namespace Platform.Infrastructure.Catalog.Channels;

public sealed class InMemoryChannelRepository : IChannelRepository
{
    private readonly InMemoryCatalogStore _store;

    public InMemoryChannelRepository(InMemoryCatalogStore store)
    {
        _store = store;
    }

    public Task<ChannelListResult> ListAsync(ListChannelsQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var filtered = ApplySorting(
            _store.Channels.Values
                .Where(x => string.IsNullOrWhiteSpace(query.Search)
                    || x.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(x.HostName) && x.HostName.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
                .Where(x => string.IsNullOrWhiteSpace(query.Status)
                    || string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase)),
            query.Sort)
            .ToList();

        return Task.FromResult(new ChannelListResult(filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), filtered.Count));
    }

    public Task<Channel?> GetByIdAsync(Guid channelId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Channels.TryGetValue(channelId, out var channel) ? channel : null);
    }

    public Task<IReadOnlyList<Channel>> GetByIdsAsync(IReadOnlyCollection<Guid> channelIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Channel> items = channelIds.Where(id => _store.Channels.ContainsKey(id)).Select(id => _store.Channels[id]).ToList();
        return Task.FromResult(items);
    }

    public Task<Channel?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Channels.Values.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Channel?> GetByHostNameAsync(string hostName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Channels.Values.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.HostName)
            && string.Equals(x.HostName, hostName, StringComparison.OrdinalIgnoreCase)));
    }

    public Task AddAsync(Channel channel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.Channels[channel.Id] = channel;
        return Task.CompletedTask;
    }

    private static IOrderedEnumerable<Channel> ApplySorting(IEnumerable<Channel> channels, string? sort)
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
