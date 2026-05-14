using Platform.Contracts.Catalog.Channels;

namespace Platform.Backoffice.Models;

public sealed class ChannelListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<ChannelSummaryDto> Channels { get; init; } = [];
    public int Total { get; init; }
}
