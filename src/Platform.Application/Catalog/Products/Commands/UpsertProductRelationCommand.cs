namespace Platform.Application.Catalog.Products.Commands;

public sealed record UpsertProductRelationCommand(
    Guid ProductId,
    Guid TargetProductId,
    string RelationType,
    decimal? Quantity,
    int SortOrder,
    string RowVersion);
