using Platform.Application.Catalog.Categories.Commands;
using Platform.Application.Catalog.Categories.Queries;
using Platform.Contracts.Catalog.Categories;
using Platform.Contracts.Common;

namespace Platform.Application.Catalog.Categories;

public interface ICategoryAdminApplicationService
{
    Task<PagedResponse<CategorySummaryDto>> ListAsync(ListCategoriesQuery query, CancellationToken cancellationToken);
    Task<CategoryDetailsDto?> GetByIdAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken);
    Task<CategoryDetailsDto> CreateAsync(CreateCategoryCommand command, CancellationToken cancellationToken);
    Task<CategoryDetailsDto?> UpdateAsync(UpdateCategoryCommand command, CancellationToken cancellationToken);
    Task<CategoryDetailsDto?> ArchiveAsync(ArchiveCategoryCommand command, CancellationToken cancellationToken);
    Task<CategoryTranslationDto?> UpsertTranslationAsync(UpsertCategoryTranslationCommand command, CancellationToken cancellationToken);
}
