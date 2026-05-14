using Microsoft.Extensions.Logging;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Integrations;
using Platform.Domain.Common;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class WebhookDeliveryExecutionService : IWebhookDeliveryExecutionService
{
    private readonly IWebhookDeliveryRepository _webhookDeliveryRepository;
    private readonly IWebhookSubscriptionRepository _webhookSubscriptionRepository;
    private readonly IWebhookSender _webhookSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookDeliveryExecutionService> _logger;

    public WebhookDeliveryExecutionService(
        IWebhookDeliveryRepository webhookDeliveryRepository,
        IWebhookSubscriptionRepository webhookSubscriptionRepository,
        IWebhookSender webhookSender,
        IUnitOfWork unitOfWork,
        ILogger<WebhookDeliveryExecutionService> logger)
    {
        _webhookDeliveryRepository = webhookDeliveryRepository;
        _webhookSubscriptionRepository = webhookSubscriptionRepository;
        _webhookSender = webhookSender;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExecutePendingAsync(int maxDeliveries, CancellationToken cancellationToken)
    {
        if (maxDeliveries <= 0)
        {
            return 0;
        }

        var processed = 0;

        for (var i = 0; i < maxDeliveries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var delivery = await _webhookDeliveryRepository.GetNextRunnableAsync(DateTime.UtcNow, cancellationToken);
            if (delivery is null)
            {
                break;
            }

            try
            {
                delivery.Start(delivery.RowVersion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyException)
            {
                continue;
            }

            var subscription = await _webhookSubscriptionRepository.GetByIdAsync(delivery.WebhookSubscriptionId, cancellationToken);
            if (subscription is null || !subscription.IsActive)
            {
                delivery.MarkAbandoned(null, "Webhook subscription is missing or inactive.", delivery.RowVersion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                processed++;
                continue;
            }

            try
            {
                var sendResult = await _webhookSender.SendAsync(subscription, delivery, cancellationToken);
                if (sendResult.IsSuccess)
                {
                    delivery.MarkSucceeded(sendResult.ResponseCode, sendResult.ResponseBody, delivery.RowVersion);
                }
                else if (IsPermanentFailure(sendResult.ResponseCode))
                {
                    delivery.MarkAbandoned(sendResult.ResponseCode, sendResult.ResponseBody, delivery.RowVersion);
                }
                else
                {
                    delivery.MarkFailed(sendResult.ResponseCode, sendResult.ResponseBody, DateTime.UtcNow.AddMinutes(1), delivery.RowVersion);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Webhook delivery {DeliveryId} execution failed.", delivery.Id);
                delivery.MarkFailed(null, exception.Message, DateTime.UtcNow.AddMinutes(1), delivery.RowVersion);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                processed++;
            }
        }

        return processed;
    }

    private static bool IsPermanentFailure(int? responseCode)
    {
        return responseCode.HasValue
            && responseCode.Value >= 400
            && responseCode.Value < 500
            && responseCode.Value is not 408 and not 429;
    }
}
