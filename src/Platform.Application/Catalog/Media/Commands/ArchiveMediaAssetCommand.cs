namespace Platform.Application.Catalog.Media.Commands;

public sealed record ArchiveMediaAssetCommand(
    Guid MediaAssetId,
    string RowVersion);
