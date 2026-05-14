using Platform.Contracts.Catalog.Channels;

namespace Platform.Backoffice.Models;

public sealed class ChannelDetailsPageViewModel
{
    public ChannelUpdateViewModel Channel { get; init; } = new();
    public IReadOnlyList<ChannelMarketAssignmentDto> Markets { get; init; } = [];
    public ChannelMarketAssignmentCreateViewModel MarketAssignmentForm { get; init; } = new();
}
