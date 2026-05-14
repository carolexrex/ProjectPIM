namespace Platform.Application.Catalog.Media.Commands;

public sealed record UpdateMediaAssetCommand(
    Guid MediaAssetId,
    string FileName,
    string ContentType,
    long FileSize,
    int? Width,
    int? Height,
    string PublicUrl,
    string? AltText,
    string? Title,
    string RowVersion);
