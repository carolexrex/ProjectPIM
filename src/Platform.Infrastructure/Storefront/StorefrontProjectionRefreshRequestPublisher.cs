using System.Text.Json;
using Platform.Application.Integrations;
using Platform.Application.Storefront;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontProjectionRefreshRequestPublisher : IStorefrontProjectionRefreshRequestPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxEventPublisher _outboxEventPublisher;

    public StorefrontProjectionRefreshRequestPublisher(IOutboxEventPublisher outboxEventPublisher)
    {
        _outboxEventPublisher = outboxEventPublisher;
    }

    public Task EnqueueProductRefreshAsync(Guid productId, string reason, CancellationToken cancellationToken)
    {
        return EnqueueAsync([productId], [], reason, productId, cancellationToken);
    }

    public Task EnqueueVariantRefreshAsync(Guid variantId, string reason, CancellationToken cancellationToken)
    {
        return EnqueueAsync([], [variantId], reason, variantId, cancellationToken);
    }

    public Task EnqueueVariantsRefreshAsync(IReadOnlyCollection<Guid> variantIds, string reason, CancellationToken cancellationToken)
    {
        var normalizedVariantIds = variantIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalizedVariantIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        return EnqueueAsync([], normalizedVariantIds, reason, normalizedVariantIds[0], cancellationToken);
    }

    private Task EnqueueAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<Guid> variantIds,
        string reason,
        Guid aggregateId,
        CancellationToken cancellationToken)
    {
        var payload = new StorefrontProjectionRefreshRequestedPayload(
            DateTime.UtcNow,
            string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason.Trim(),
            productIds.Where(x => x != Guid.Empty).Distinct().ToList(),
            variantIds.Where(x => x != Guid.Empty).Distinct().ToList());

        if (payload.ProductIds.Count == 0 && payload.VariantIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        return _outboxEventPublisher.EnqueueAsync(
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            "StorefrontProductProjection",
            aggregateId,
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);
    }
}
