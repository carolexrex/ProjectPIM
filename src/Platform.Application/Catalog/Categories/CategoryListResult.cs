using Platform.Domain.Catalog.Categories;

namespace Platform.Application.Catalog.Categories;

public sealed record CategoryListResult(IReadOnlyList<Category> Items, int Total);
