namespace Platform.Application.Catalog.Variants.Commands;

public sealed record RemoveVariantMediaCommand(
    Guid VariantId,
    Guid VariantMediaId,
    string RowVersion);
