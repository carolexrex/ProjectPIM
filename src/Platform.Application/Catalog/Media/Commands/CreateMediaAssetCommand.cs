namespace Platform.Application.Catalog.Media.Commands;

public sealed record CreateMediaAssetCommand(
    string StorageProvider,
    string StorageKey,
    string FileName,
    string ContentType,
    long FileSize,
    int? Width,
    int? Height,
    string PublicUrl,
    string? AltText,
    string? Title);
