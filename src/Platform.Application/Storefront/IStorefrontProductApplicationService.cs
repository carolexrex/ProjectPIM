namespace Platform.Application.Storefront;

public interface IStorefrontProductApplicationService
{
    Task<StorefrontProductListResult> ListAsync(GetStorefrontProductsQuery query, CancellationToken cancellationToken);
    Task<StorefrontProductDetailsResult> GetBySlugAsync(GetStorefrontProductBySlugQuery query, CancellationToken cancellationToken);
    Task<StorefrontProductDetailsResult> GetByProductNumberAsync(GetStorefrontProductByProductNumberQuery query, CancellationToken cancellationToken);
}
