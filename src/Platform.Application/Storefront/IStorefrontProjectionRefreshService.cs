namespace Platform.Application.Storefront;

public interface IStorefrontProjectionRefreshService
{
    Task RefreshProductAsync(Guid productId, CancellationToken cancellationToken);
    Task RefreshProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task RebuildAllAsync(CancellationToken cancellationToken);
}
