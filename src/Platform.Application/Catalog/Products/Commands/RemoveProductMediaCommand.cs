namespace Platform.Application.Catalog.Products.Commands;

public sealed record RemoveProductMediaCommand(
    Guid ProductId,
    Guid ProductMediaId,
    string RowVersion);
