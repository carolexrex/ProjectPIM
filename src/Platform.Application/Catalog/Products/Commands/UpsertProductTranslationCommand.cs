namespace Platform.Application.Catalog.Products.Commands;

public sealed record UpsertProductTranslationCommand(
    Guid ProductId,
    string CultureCode,
    string Name,
    string? ShortDescription,
    string? LongDescription,
    string? SeoTitle,
    string? SeoDescription);
