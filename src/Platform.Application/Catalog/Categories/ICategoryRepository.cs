using Platform.Application.Catalog.Categories.Queries;
using Platform.Domain.Catalog.Categories;

namespace Platform.Application.Catalog.Categories;

public interface ICategoryRepository
{
    Task<CategoryListResult> ListAsync(ListCategoriesQuery query, CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetByIdsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);
    Task<Category?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task AddAsync(Category category, CancellationToken cancellationToken);
}
