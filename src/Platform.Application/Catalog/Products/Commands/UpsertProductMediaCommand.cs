namespace Platform.Application.Catalog.Products.Commands;

public sealed record UpsertProductMediaCommand(
    Guid ProductId,
    Guid MediaAssetId,
    string Type,
    int SortOrder,
    bool IsPrimary,
    string RowVersion);
