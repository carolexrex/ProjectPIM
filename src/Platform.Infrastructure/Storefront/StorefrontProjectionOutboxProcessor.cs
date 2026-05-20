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
    private const int MaxProcessingAttempts = 5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IStorefrontProjectionRefreshService _refreshService;
    private readonly IVariantRepository _variantRepository;
    private readonly IStorefrontProjectionChangeTracker _projectionChangeTracker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StorefrontProjectionOutboxProcessor> _logger;

    public StorefrontProjectionOutboxProcessor(
        IOutboxMessageRepository outboxMessageRepository,
        IStorefrontProjectionRefreshService refreshService,
        IVariantRepository variantRepository,
        IStorefrontProjectionChangeTracker projectionChangeTracker,
        IUnitOfWork unitOfWork,
        ILogger<StorefrontProjectionOutboxProcessor> logger)
    {
        _outboxMessageRepository = outboxMessageRepository;
        _refreshService = refreshService;
        _variantRepository = variantRepository;
        _projectionChangeTracker = projectionChangeTracker;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExecutePendingAsync(int maxMessages, CancellationToken cancellationToken)
    {
        if (maxMessages <= 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var messages = await _outboxMessageRepository.ListRunnableByEventTypeAsync(
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            maxMessages,
            now,
            cancellationToken);
        if (messages.Count == 0)
        {
            return 0;
        }

        var distinctProductIds = new List<Guid>();

        try
        {
            var productIds = new List<Guid>();

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                productIds.AddRange(await ResolveProductIdsAsync(message, cancellationToken));
            }

            distinctProductIds = productIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (distinctProductIds.Count > 0)
            {
                await _refreshService.RefreshProductsAsync(distinctProductIds, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _projectionChangeTracker.DiscardPendingChanges();
            _logger.LogWarning(
                exception,
                "Storefront projection refresh batch failed. Falling back to per-message processing for {MessageCount} message(s).",
                messages.Count);

            return await ProcessIndividuallyAsync(messages, cancellationToken);
        }

        var processed = 0;
        foreach (var message in messages)
        {
            if (MarkPublished(message))
            {
                processed++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Processed {MessageCount} storefront projection refresh request(s) for {ProductCount} distinct product(s).",
            processed,
            distinctProductIds.Count);

        return processed;
    }

    private async Task<int> ProcessIndividuallyAsync(
        IReadOnlyList<OutboxMessage> messages,
        CancellationToken cancellationToken)
    {
        var processed = 0;
        var refreshedProductCount = 0;

        foreach (var message in messages)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var productIds = (await ResolveProductIdsAsync(message, cancellationToken))
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (productIds.Count > 0)
                {
                    await _refreshService.RefreshProductsAsync(productIds, cancellationToken);
                    refreshedProductCount += productIds.Count;
                }

                if (MarkPublished(message))
                {
                    processed++;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _projectionChangeTracker.DiscardPendingChanges();
                await MarkFailedAsync([message], exception, cancellationToken);
            }
        }

        if (processed > 0)
        {
            _logger.LogInformation(
                "Processed {MessageCount} storefront projection refresh request(s) for {ProductCount} product refresh operation(s) after batch fallback.",
                processed,
                refreshedProductCount);
        }

        return processed;
    }

    private bool MarkPublished(OutboxMessage message)
    {
        try
        {
            message.MarkPublished(message.RowVersion);
            return true;
        }
        catch (ConcurrencyException exception)
        {
            _logger.LogDebug(exception, "Storefront projection refresh message {MessageId} was already updated.", message.Id);
            return false;
        }
    }

    private async Task MarkFailedAsync(
        IReadOnlyList<OutboxMessage> messages,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var abandonedCount = 0;

        foreach (var message in messages)
        {
            try
            {
                if (message.ProcessingAttemptCount + 1 >= MaxProcessingAttempts)
                {
                    message.MarkProcessingAbandoned(exception.Message, message.RowVersion);
                    abandonedCount++;
                }
                else
                {
                    message.MarkProcessingRetry(
                        exception.Message,
                        DateTime.UtcNow.Add(ResolveBackoffDelay(message.ProcessingAttemptCount + 1)),
                        message.RowVersion);
                    retryCount++;
                }
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogDebug(concurrencyException, "Storefront projection refresh message {MessageId} was already updated.", message.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (abandonedCount > 0)
        {
            _logger.LogError(
                exception,
                "Storefront projection refresh failed. Scheduled {RetryCount} message(s) for retry and abandoned {AbandonedCount} message(s).",
                retryCount,
                abandonedCount);
            return;
        }

        _logger.LogWarning(
            exception,
            "Storefront projection refresh failed. Scheduled {RetryCount} message(s) for retry.",
            retryCount);
    }

    private static TimeSpan ResolveBackoffDelay(int attemptCount)
    {
        var minutes = Math.Min(30, Math.Pow(2, Math.Max(0, attemptCount - 1)));
        return TimeSpan.FromMinutes(minutes);
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
