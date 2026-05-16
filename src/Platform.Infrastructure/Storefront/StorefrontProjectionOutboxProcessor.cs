using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Variants;
using Platform.Application.Integrations;
using Platform.Application.Storefront;
using Platform.Domain.Common;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontProjectionOutboxProcessor : IStorefrontProjectionOutboxProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IStorefrontProjectionRefreshService _refreshService;
    private readonly IVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StorefrontProjectionOutboxProcessor> _logger;

    public StorefrontProjectionOutboxProcessor(
        IOutboxMessageRepository outboxMessageRepository,
        IStorefrontProjectionRefreshService refreshService,
        IVariantRepository variantRepository,
        IUnitOfWork unitOfWork,
        ILogger<StorefrontProjectionOutboxProcessor> logger)
    {
        _outboxMessageRepository = outboxMessageRepository;
        _refreshService = refreshService;
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExecutePendingAsync(int maxMessages, CancellationToken cancellationToken)
    {
        if (maxMessages <= 0)
        {
            return 0;
        }

        var processed = 0;

        for (var i = 0; i < maxMessages; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await _outboxMessageRepository.GetNextUnpublishedByEventTypeAsync(
                WebhookEventTypes.StorefrontProjectionRefreshRequested,
                cancellationToken);
            if (message is null)
            {
                break;
            }

            try
            {
                var productIds = await ResolveProductIdsAsync(message, cancellationToken);
                if (productIds.Count > 0)
                {
                    await _refreshService.RefreshProductsAsync(productIds, cancellationToken);
                }

                message.MarkPublished(message.RowVersion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (ConcurrencyException)
            {
                continue;
            }
        }

        return processed;
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveProductIdsAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        StorefrontProjectionRefreshRequestedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<StorefrontProjectionRefreshRequestedPayload>(message.PayloadJson, JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Storefront projection refresh message {MessageId} has an invalid payload.", message.Id);
            return [];
        }

        if (payload is null)
        {
            return [];
        }

        var productIds = payload.ProductIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var variantIds = payload.VariantIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (variantIds.Count > 0)
        {
            var variants = await _variantRepository.GetByIdsAsync(variantIds, cancellationToken);
            productIds.AddRange(variants.Select(x => x.ProductId));
        }

        return productIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
    }
}
