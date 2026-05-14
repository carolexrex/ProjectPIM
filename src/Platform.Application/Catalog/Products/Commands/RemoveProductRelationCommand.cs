namespace Platform.Application.Catalog.Products.Commands;

public sealed record RemoveProductRelationCommand(
    Guid ProductId,
    Guid RelationId,
    string RowVersion);
