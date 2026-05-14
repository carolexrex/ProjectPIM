using Platform.Application.Catalog.Channels.Commands;
using Platform.Application.Catalog.Channels.Queries;
using Platform.Contracts.Catalog.Channels;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Channels;

public interface IChannelAdminApplicationService
{
    Task<PagedResponse<ChannelSummaryDto>> ListAsync(ListChannelsQuery query, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> GetByIdAsync(GetChannelByIdQuery query, CancellationToken cancellationToken);
    Task<ChannelDetailsDto> CreateAsync(CreateChannelCommand command, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> UpdateAsync(UpdateChannelCommand command, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> ArchiveAsync(ArchiveChannelCommand command, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> UpsertMarketAssignmentAsync(UpsertChannelMarketAssignmentCommand command, CancellationToken cancellationToken);
    Task<ChannelDetailsDto?> RemoveMarketAssignmentAsync(RemoveChannelMarketAssignmentCommand command, CancellationToken cancellationToken);
}
