namespace Platform.Application.Catalog.Categories.Commands;

public sealed record CreateCategoryCommand(
    string Code,
    Guid? ParentCategoryId,
    int SortOrder);
