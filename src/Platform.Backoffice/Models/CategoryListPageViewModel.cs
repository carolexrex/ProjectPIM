using Platform.Contracts.Catalog.Categories;

namespace Platform.Backoffice.Models;

public sealed class CategoryListPageViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<CategoryListItemViewModel> Categories { get; init; } = [];
    public int Total { get; init; }
}
