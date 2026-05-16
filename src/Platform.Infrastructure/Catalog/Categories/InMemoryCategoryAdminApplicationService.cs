using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Categories.Commands;
using Platform.Application.Catalog.Categories.Queries;
using Platform.Application.Catalog.Products;
using Platform.Application.Storefront;
using Platform.Contracts.Catalog.Categories;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Categories;

namespace Platform.Infrastructure.Catalog.Categories;

public sealed class InMemoryCategoryAdminApplicationService : ICategoryAdminApplicationService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStorefrontProjectionRefreshRequestPublisher _storefrontProjectionRefreshRequestPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public InMemoryCategoryAdminApplicationService(
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IStorefrontProjectionRefreshRequestPublisher storefrontProjectionRefreshRequestPublisher,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _storefrontProjectionRefreshRequestPublisher = storefrontProjectionRefreshRequestPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<CategorySummaryDto>> ListAsync(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        var result = await _categoryRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        return new PagedResponse<CategorySummaryDto>(result.Items.Select(MapSummary).ToList(), result.Total, page, pageSize);
    }

    public async Task<CategoryDetailsDto?> GetByIdAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(query.CategoryId, cancellationToken);
        return category is null ? null : MapDetails(category);
    }

    public async Task<CategoryDetailsDto> CreateAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeIsUniqueAsync(command.Code, null, cancellationToken);
        await EnsureParentExistsAsync(command.ParentCategoryId, null, cancellationToken);

        var now = DateTime.UtcNow;
        var category = new Category(Guid.NewGuid(), command.Code, command.ParentCategoryId, command.SortOrder, now, now);

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(category);
    }

    public async Task<CategoryDetailsDto?> UpdateAsync(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        await EnsureCodeIsUniqueAsync(command.Code, command.CategoryId, cancellationToken);
        await EnsureParentExistsAsync(command.ParentCategoryId, command.CategoryId, cancellationToken);

        category.Update(command.Code, command.ParentCategoryId, command.SortOrder, command.RowVersion);
        await EnqueueStorefrontRefreshAsync(category.Id, "CategoryUpdated", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(category);
    }

    public async Task<CategoryDetailsDto?> ArchiveAsync(ArchiveCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        category.Archive();
        await EnqueueStorefrontRefreshAsync(category.Id, "CategoryArchived", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(category);
    }

    public async Task<CategoryTranslationDto?> UpsertTranslationAsync(UpsertCategoryTranslationCommand command, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var translation = category.UpsertTranslation(command.CultureCode, command.Name, command.Slug, command.Description);
        await EnqueueStorefrontRefreshAsync(category.Id, "CategoryTranslationUpserted", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTranslation(translation);
    }

    private async Task EnqueueStorefrontRefreshAsync(Guid categoryId, string reason, CancellationToken cancellationToken)
    {
        var categoryIds = await _categoryRepository.ListSubtreeIdsAsync(categoryId, cancellationToken);
        var productIds = await _productRepository.ListIdsByCategoryIdsAsync(categoryIds, cancellationToken);
        foreach (var productId in productIds)
        {
            await _storefrontProjectionRefreshRequestPublisher.EnqueueProductRefreshAsync(productId, reason, cancellationToken);
        }
    }

    private async Task EnsureCodeIsUniqueAsync(string code, Guid? currentCategoryId, CancellationToken cancellationToken)
    {
        var existing = await _categoryRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != currentCategoryId)
        {
            throw new ConflictException("Category code already exists.");
        }
    }

    private async Task EnsureParentExistsAsync(Guid? parentCategoryId, Guid? currentCategoryId, CancellationToken cancellationToken)
    {
        if (parentCategoryId is null)
        {
            return;
        }

        if (parentCategoryId == currentCategoryId)
        {
            throw new RequestValidationException(nameof(UpdateCategoryCommand.ParentCategoryId), "A category cannot be its own parent.");
        }

        var parent = await _categoryRepository.GetByIdAsync(parentCategoryId.Value, cancellationToken);
        if (parent is null)
        {
            throw new RequestValidationException(nameof(CreateCategoryCommand.ParentCategoryId), "Unknown parent category.");
        }

        if (currentCategoryId is not Guid categoryId)
        {
            return;
        }

        var visited = new HashSet<Guid>();
        var currentParent = parent;

        while (currentParent is not null)
        {
            if (!visited.Add(currentParent.Id))
            {
                throw new RequestValidationException(nameof(UpdateCategoryCommand.ParentCategoryId), "Category hierarchy contains a cycle.");
            }

            if (currentParent.Id == categoryId)
            {
                throw new RequestValidationException(nameof(UpdateCategoryCommand.ParentCategoryId), "A category cannot be moved under one of its descendants.");
            }

            currentParent = currentParent.ParentCategoryId is Guid nextParentId
                ? await _categoryRepository.GetByIdAsync(nextParentId, cancellationToken)
                : null;
        }
    }

    private static CategorySummaryDto MapSummary(Category category)
    {
        return new CategorySummaryDto(
            category.Id,
            category.Code,
            category.Translations.FirstOrDefault()?.Name,
            category.ParentCategoryId,
            category.SortOrder,
            category.Status,
            category.CreatedAtUtc,
            category.UpdatedAtUtc,
            category.RowVersion);
    }

    private static CategoryDetailsDto MapDetails(Category category)
    {
        return new CategoryDetailsDto(
            category.Id,
            category.Code,
            category.ParentCategoryId,
            category.SortOrder,
            category.Status,
            category.Translations.Select(MapTranslation).ToList(),
            category.CreatedAtUtc,
            category.UpdatedAtUtc,
            category.RowVersion);
    }

    private static CategoryTranslationDto MapTranslation(CategoryTranslation translation)
    {
        return new CategoryTranslationDto(translation.CultureCode, translation.Name, translation.Slug, translation.Description);
    }
}
