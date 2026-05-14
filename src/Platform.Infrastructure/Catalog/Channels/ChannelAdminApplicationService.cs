using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Channels;
using Platform.Application.Catalog.Channels.Commands;
using Platform.Application.Catalog.Channels.Queries;
using Platform.Application.Catalog.Markets;
using Platform.Contracts.Catalog.Channels;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Channels;

namespace Platform.Infrastructure.Catalog.Channels;

public sealed class ChannelAdminApplicationService : IChannelAdminApplicationService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChannelAdminApplicationService(
        IChannelRepository channelRepository,
        IMarketRepository marketRepository,
        IUnitOfWork unitOfWork)
    {
        _channelRepository = channelRepository;
        _marketRepository = marketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<ChannelSummaryDto>> ListAsync(ListChannelsQuery query, CancellationToken cancellationToken)
    {
        var result = await _channelRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        return new PagedResponse<ChannelSummaryDto>(result.Items.Select(MapSummary).ToList(), result.Total, page, pageSize);
    }

    public async Task<ChannelDetailsDto?> GetByIdAsync(GetChannelByIdQuery query, CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetByIdAsync(query.ChannelId, cancellationToken);
        return channel is null ? null : await MapDetailsAsync(channel, cancellationToken);
    }

    public async Task<ChannelDetailsDto> CreateAsync(CreateChannelCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeUniqueAsync(command.Code, null, cancellationToken);
        var now = DateTime.UtcNow;
        var channel = new Channel(Guid.NewGuid(), command.Code, command.Name, command.HostName, now, now);
        await _channelRepository.AddAsync(channel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(channel, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> UpdateAsync(UpdateChannelCommand command, CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetByIdAsync(command.ChannelId, cancellationToken);
        if (channel is null)
        {
            return null;
        }

        await EnsureCodeUniqueAsync(command.Code, command.ChannelId, cancellationToken);
        channel.Update(command.Code, command.Name, command.HostName, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(channel, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> ArchiveAsync(ArchiveChannelCommand command, CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetByIdAsync(command.ChannelId, cancellationToken);
        if (channel is null)
        {
            return null;
        }

        channel.Archive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(channel, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> UpsertMarketAssignmentAsync(UpsertChannelMarketAssignmentCommand command, CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetByIdAsync(command.ChannelId, cancellationToken);
        if (channel is null)
        {
            return null;
        }

        if (await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(UpsertChannelMarketAssignmentCommand.MarketId), "Unknown market.");
        }

        channel.UpsertMarketAssignment(command.MarketId, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(channel, cancellationToken);
    }

    public async Task<ChannelDetailsDto?> RemoveMarketAssignmentAsync(RemoveChannelMarketAssignmentCommand command, CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetByIdAsync(command.ChannelId, cancellationToken);
        if (channel is null)
        {
            return null;
        }

        channel.RemoveMarketAssignment(command.MarketId, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetailsAsync(channel, cancellationToken);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? currentChannelId, CancellationToken cancellationToken)
    {
        var existing = await _channelRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != currentChannelId)
        {
            throw new ConflictException("Channel code already exists.");
        }
    }

    private static ChannelSummaryDto MapSummary(Channel channel)
    {
        return new ChannelSummaryDto(channel.Id, channel.Code, channel.Name, channel.HostName, channel.Status, channel.UpdatedAtUtc, channel.RowVersion);
    }

    private async Task<ChannelDetailsDto> MapDetailsAsync(Channel channel, CancellationToken cancellationToken)
    {
        var marketIds = channel.MarketAssignments.Select(x => x.MarketId).Distinct().ToList();
        var markets = marketIds.Count == 0 ? [] : await _marketRepository.GetByIdsAsync(marketIds, cancellationToken);
        var marketMap = markets.ToDictionary(x => x.Id);

        return new ChannelDetailsDto(
            channel.Id,
            channel.Code,
            channel.Name,
            channel.HostName,
            channel.Status,
            channel.MarketAssignments.Select(x =>
            {
                var market = marketMap.GetValueOrDefault(x.MarketId);
                return new ChannelMarketAssignmentDto(x.MarketId, market?.Code ?? x.MarketId.ToString(), market?.Name ?? x.MarketId.ToString());
            }).OrderBy(x => x.MarketCode).ToList(),
            channel.CreatedAtUtc,
            channel.UpdatedAtUtc,
            channel.RowVersion);
    }
}
