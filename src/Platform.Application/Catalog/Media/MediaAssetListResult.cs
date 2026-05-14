using Platform.Domain.Catalog.Media;

namespace Platform.Application.Catalog.Media;

public sealed record MediaAssetListResult(IReadOnlyList<MediaAsset> Items, int Total);
