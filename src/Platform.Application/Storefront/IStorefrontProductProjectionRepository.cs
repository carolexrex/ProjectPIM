namespace Platform.Application.Storefront;

public interface IStorefrontProductProjectionRepository
{
    Task<IReadOnlyList<StorefrontProductProjection>> ListByContextAsync(string marketCode, string cultureCode, string currencyCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<StorefrontProductProjection>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<StorefrontProductProjection?> GetBySlugAsync(string marketCode, string cultureCode, string currencyCode, string slug, CancellationToken cancellationToken);
    Task<StorefrontProductProjection?> GetByProductNumberAsync(string marketCode, string cultureCode, string currencyCode, string productNumber, CancellationToken cancellationToken);
    Task ReplaceForProductAsync(Guid productId, IReadOnlyCollection<StorefrontProductProjection> projections, CancellationToken cancellationToken);
    Task DeleteAllAsync(CancellationToken cancellationToken);
}
