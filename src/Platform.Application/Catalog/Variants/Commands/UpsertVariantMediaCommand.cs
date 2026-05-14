namespace Platform.Application.Catalog.Variants.Commands;

public sealed record UpsertVariantMediaCommand(
    Guid VariantId,
    Guid MediaAssetId,
    string Type,
    int SortOrder,
    bool IsPrimary,
    string RowVersion);
