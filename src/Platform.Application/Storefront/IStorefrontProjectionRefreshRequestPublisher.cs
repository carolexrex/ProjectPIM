namespace Platform.Application.Storefront;

public interface IStorefrontProjectionRefreshRequestPublisher
{
    Task EnqueueProductRefreshAsync(Guid productId, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues a single-product refresh under a scheduling identity so a producer that recomputes
    /// the same work — such as the time-driven price-validity scan — cannot stack duplicates.
    /// Throws <see cref="Platform.Domain.Integrations.DuplicateOutboxDedupKeyException"/> when an
    /// open request already holds the key.
    /// </summary>
    Task EnqueueDeduplicatedProductRefreshAsync(Guid productId, string reason, string dedupKey, CancellationToken cancellationToken);

    Task EnqueueVariantRefreshAsync(Guid variantId, string reason, CancellationToken cancellationToken);
    Task EnqueueVariantsRefreshAsync(IReadOnlyCollection<Guid> variantIds, string reason, CancellationToken cancellationToken);
}
