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

        var messages = await _outboxMessageRepository.ListUnpublishedByEventTypeAsync(
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            maxMessages,
            cancellationToken);
        if (messages.Count == 0)
        {
            return 0;
        }

        var productIds = new List<Guid>();

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            productIds.AddRange(await ResolveProductIdsAsync(message, cancellationToken));
        }

        var distinctProductIds = productIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctProductIds.Count > 0)
        {
            await _refreshService.RefreshProductsAsync(distinctProductIds, cancellationToken);
        }

        var processed = 0;
        foreach (var message in messages)
        {
            try
            {
                message.MarkPublished(message.RowVersion);
                processed++;
            }
            catch (ConcurrencyException exception)
            {
                _logger.LogDebug(exception, "Storefront projection refresh message {MessageId} was already updated.", message.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Processed {MessageCount} storefront projection refresh request(s) for {ProductCount} distinct product(s).",
            processed,
            distinctProductIds.Count);

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
