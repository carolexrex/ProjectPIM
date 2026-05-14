using Platform.Contracts.Storefront;

namespace Platform.Application.Storefront;

public interface IStorefrontCategoryApplicationService
{
    Task<StorefrontCategoryListResult> ListAsync(GetStorefrontCategoriesQuery query, CancellationToken cancellationToken);
    Task<StorefrontCategoryDetailsResult> GetBySlugAsync(GetStorefrontCategoryBySlugQuery query, CancellationToken cancellationToken);
}
