using Platform.Contracts.Integrations;

namespace Platform.Backoffice.Models;

public sealed class StorefrontRefreshMessageListPageViewModel
{
    public string Status { get; init; } = "open";
    public string Sort { get; init; } = "occurredAtUtc";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public IReadOnlyList<StorefrontProjectionRefreshMessageSummaryDto> Messages { get; init; } = [];
    public int Total { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(1, PageSize)));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
