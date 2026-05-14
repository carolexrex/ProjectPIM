using Platform.Contracts.Catalog.Media;

namespace Platform.Backoffice.Models;

public sealed class MediaAssetDetailsPageViewModel
{
    public MediaAssetUpdateViewModel Asset { get; init; } = new();
    public MediaAssetDetailsDto Details { get; init; } = default!;
}
