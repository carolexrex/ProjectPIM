namespace Platform.Application.Catalog.Categories.Commands;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Code,
    Guid? ParentCategoryId,
    int SortOrder,
    string RowVersion);
