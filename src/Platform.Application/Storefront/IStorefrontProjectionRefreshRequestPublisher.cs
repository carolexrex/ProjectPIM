namespace Platform.Application.Storefront;

public interface IStorefrontProjectionRefreshRequestPublisher
{
    Task EnqueueProductRefreshAsync(Guid productId, string reason, CancellationToken cancellationToken);
    Task EnqueueVariantRefreshAsync(Guid variantId, string reason, CancellationToken cancellationToken);
    Task EnqueueVariantsRefreshAsync(IReadOnlyCollection<Guid> variantIds, string reason, CancellationToken cancellationToken);
}
