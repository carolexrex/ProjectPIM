using Microsoft.Extensions.Logging;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Integrations;
using Platform.Domain.Common;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class WebhookOutboxExecutionService : IWebhookOutboxExecutionService
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IWebhookSubscriptionRepository _webhookSubscriptionRepository;
    private readonly IWebhookDeliveryRepository _webhookDeliveryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookOutboxExecutionService> _logger;

    public WebhookOutboxExecutionService(
        IOutboxMessageRepository outboxMessageRepository,
        IWebhookSubscriptionRepository webhookSubscriptionRepository,
        IWebhookDeliveryRepository webhookDeliveryRepository,
        IUnitOfWork unitOfWork,
        ILogger<WebhookOutboxExecutionService> logger)
    {
        _outboxMessageRepository = outboxMessageRepository;
        _webhookSubscriptionRepository = webhookSubscriptionRepository;
        _webhookDeliveryRepository = webhookDeliveryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExecutePendingAsync(int maxMessages, CancellationToken cancellationToken)
    {
        if (maxMessages <= 0)
        {
            return 0;
        }

        var published = 0;

        for (var i = 0; i < maxMessages; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = await _outboxMessageRepository.GetNextUnpublishedAsync(cancellationToken);
            if (message is null)
            {
                break;
            }

            try
            {
                var subscriptions = await _webhookSubscriptionRepository.ListActiveByEventTypeAsync(message.EventType, cancellationToken);
                foreach (var subscription in subscriptions)
                {
                    await _webhookDeliveryRepository.AddAsync(
                        new WebhookDelivery(
                            Guid.NewGuid(),
                            subscription.Id,
                            message.Id,
                            message.EventType,
                            message.PayloadJson,
                            DateTime.UtcNow),
                        cancellationToken);
                }

                message.MarkPublished(message.RowVersion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                published++;
            }
            catch (ConcurrencyException)
            {
                continue;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox message {MessageId} publish failed.", message.Id);
                throw;
            }
        }

        return published;
    }
}
