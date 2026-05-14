namespace Platform.Application.Catalog.Categories.Queries;

public sealed record ListCategoriesQuery(
    string? Search,
    string? Status,
    Guid? ParentCategoryId,
    int Page,
    int PageSize,
    string? Sort);
