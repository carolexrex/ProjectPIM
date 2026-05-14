namespace Platform.Application.Storefront;

public interface IStorefrontProjectionBuilder
{
    Task<IReadOnlyList<StorefrontProductProjection>> BuildForProductAsync(Guid productId, CancellationToken cancellationToken);
}
