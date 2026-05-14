using Platform.Application.Catalog.Channels.Queries;
using Platform.Domain.Catalog.Channels;

namespace Platform.Application.Catalog.Channels;

public interface IChannelRepository
{
    Task<ChannelListResult> ListAsync(ListChannelsQuery query, CancellationToken cancellationToken);
    Task<Channel?> GetByIdAsync(Guid channelId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Channel>> GetByIdsAsync(IReadOnlyCollection<Guid> channelIds, CancellationToken cancellationToken);
    Task<Channel?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<Channel?> GetByHostNameAsync(string hostName, CancellationToken cancellationToken);
    Task AddAsync(Channel channel, CancellationToken cancellationToken);
}
