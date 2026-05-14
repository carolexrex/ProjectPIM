namespace Platform.Contracts.Catalog.Media;

public sealed record MediaAssetSummaryDto(
    Guid Id,
    string FileName,
    string ContentType,
    string PublicUrl,
    string? Title,
    string? AltText,
    string Status,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record MediaAssetDetailsDto(
    Guid Id,
    string StorageProvider,
    string StorageKey,
    string FileName,
    string ContentType,
    long FileSize,
    int? Width,
    int? Height,
    string PublicUrl,
    string? Title,
    string? AltText,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);
