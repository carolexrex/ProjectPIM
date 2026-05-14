using Platform.Contracts.Catalog.Media;

namespace Platform.Backoffice.Models;

public sealed class MediaAssetListPageViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? ContentType { get; set; }
    public int Total { get; set; }
    public IReadOnlyList<MediaAssetSummaryDto> Assets { get; set; } = [];
}
