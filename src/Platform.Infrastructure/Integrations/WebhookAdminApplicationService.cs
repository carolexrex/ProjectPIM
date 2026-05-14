using Microsoft.Extensions.Options;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class WebhookAdminApplicationService : IWebhookAdminApplicationService
{
    private readonly IWebhookSubscriptionRepository _webhookSubscriptionRepository;
    private readonly IWebhookDeliveryRepository _webhookDeliveryRepository;
    private readonly IOptions<WebhookReplayOptions> _replayOptions;
    private readonly IUnitOfWork _unitOfWork;

    public WebhookAdminApplicationService(
        IWebhookSubscriptionRepository webhookSubscriptionRepository,
        IWebhookDeliveryRepository webhookDeliveryRepository,
        IOptions<WebhookReplayOptions> replayOptions,
        IUnitOfWork unitOfWork)
    {
        _webhookSubscriptionRepository = webhookSubscriptionRepository;
        _webhookDeliveryRepository = webhookDeliveryRepository;
        _replayOptions = replayOptions;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<WebhookSubscriptionSummaryDto>> ListSubscriptionsAsync(ListWebhookSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        var result = await _webhookSubscriptionRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<WebhookSubscriptionSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<WebhookSubscriptionDetailsDto?> GetSubscriptionByIdAsync(GetWebhookSubscriptionByIdQuery query, CancellationToken cancellationToken)
    {
        var subscription = await _webhookSubscriptionRepository.GetByIdAsync(query.WebhookSubscriptionId, cancellationToken);
        return subscription is null ? null : MapDetails(subscription);
    }

    public async Task<WebhookSubscriptionDetailsDto> CreateSubscriptionAsync(CreateWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ValidateSubscriptionCommand(command.Name, command.EndpointUrl, command.Secret, command.EventTypes);

        var subscription = new WebhookSubscription(
            Guid.NewGuid(),
            command.Name,
            command.EndpointUrl,
            command.Secret,
            command.EventTypes,
            command.IsActive,
            DateTime.UtcNow);

        await _webhookSubscriptionRepository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(subscription);
    }

    public async Task<WebhookSubscriptionDetailsDto?> UpdateSubscriptionAsync(UpdateWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _webhookSubscriptionRepository.GetByIdAsync(command.WebhookSubscriptionId, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        ValidateSubscriptionCommand(command.Name, command.EndpointUrl, command.Secret, command.EventTypes);
        subscription.Update(command.Name, command.EndpointUrl, command.Secret, command.EventTypes, command.IsActive, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(subscription);
    }

    public async Task<PagedResponse<WebhookDeliverySummaryDto>> ListDeliveriesAsync(ListWebhookDeliveriesQuery query, CancellationToken cancellationToken)
    {
        var result = await _webhookDeliveryRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<WebhookDeliverySummaryDto>(
            result.Items.Select(MapDeliverySummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<WebhookDeliveryDetailsDto?> GetDeliveryByIdAsync(GetWebhookDeliveryByIdQuery query, CancellationToken cancellationToken)
    {
        var delivery = await _webhookDeliveryRepository.GetByIdAsync(query.WebhookDeliveryId, cancellationToken);
        return delivery is null ? null : MapDeliveryDetails(delivery);
    }

    public async Task<WebhookDeliveryDetailsDto?> ReplayDeliveryAsync(ReplayWebhookDeliveryCommand command, CancellationToken cancellationToken)
    {
        if (!_replayOptions.Value.ManualReplayEnabled)
        {
            throw new RequestValidationException(nameof(WebhookReplayOptions.ManualReplayEnabled), "Manual webhook replay is disabled.");
        }

        var delivery = await _webhookDeliveryRepository.GetByIdAsync(command.WebhookDeliveryId, cancellationToken);
        if (delivery is null)
        {
            return null;
        }

        if (!string.Equals(delivery.Status, WebhookDeliveryStatuses.Failed, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(delivery.Status, WebhookDeliveryStatuses.Abandoned, StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(command.WebhookDeliveryId), "Only failed or abandoned deliveries can be replayed.");
        }

        var nextAttemptAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(0, _replayOptions.Value.ManualReplayDelaySeconds));
        delivery.Replay(nextAttemptAtUtc, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDeliveryDetails(delivery);
    }

    private static void ValidateSubscriptionCommand(string name, string endpointUrl, string secret, IReadOnlyList<string> eventTypes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RequestValidationException(nameof(name), "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            throw new RequestValidationException(nameof(endpointUrl), "EndpointUrl is required.");
        }

        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpointUri)
            || (endpointUri.Scheme != Uri.UriSchemeHttp && endpointUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new RequestValidationException(nameof(endpointUrl), "EndpointUrl must be a valid absolute http or https URL.");
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new RequestValidationException(nameof(secret), "Secret is required.");
        }

        if (eventTypes.Count == 0)
        {
            throw new RequestValidationException(nameof(eventTypes), "At least one event type is required.");
        }

        var normalizedEventTypes = eventTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedEventTypes.Count != eventTypes.Count || normalizedEventTypes.Any(x => !WebhookEventTypes.All.Contains(x)))
        {
            throw new RequestValidationException(nameof(eventTypes), "EventTypes contains one or more unsupported values.");
        }
    }

    private static WebhookSubscriptionSummaryDto MapSummary(WebhookSubscription subscription)
    {
        return new WebhookSubscriptionSummaryDto(
            subscription.Id,
            subscription.Name,
            subscription.EndpointUrl,
            subscription.IsActive,
            subscription.GetEventTypes(),
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc,
            subscription.RowVersion);
    }

    private static WebhookSubscriptionDetailsDto MapDetails(WebhookSubscription subscription)
    {
        return new WebhookSubscriptionDetailsDto(
            subscription.Id,
            subscription.Name,
            subscription.EndpointUrl,
            subscription.IsActive,
            subscription.GetEventTypes(),
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc,
            subscription.RowVersion);
    }

    private static WebhookDeliverySummaryDto MapDeliverySummary(WebhookDelivery delivery)
    {
        return new WebhookDeliverySummaryDto(
            delivery.Id,
            delivery.WebhookSubscriptionId,
            delivery.EventId,
            delivery.EventType,
            delivery.Status,
            delivery.AttemptCount,
            delivery.LastAttemptAtUtc,
            delivery.NextAttemptAtUtc,
            delivery.ResponseCode,
            delivery.CreatedAtUtc,
            delivery.UpdatedAtUtc,
            delivery.RowVersion);
    }

    private static WebhookDeliveryDetailsDto MapDeliveryDetails(WebhookDelivery delivery)
    {
        return new WebhookDeliveryDetailsDto(
            delivery.Id,
            delivery.WebhookSubscriptionId,
            delivery.EventId,
            delivery.EventType,
            delivery.Status,
            delivery.PayloadJson,
            delivery.AttemptCount,
            delivery.LastAttemptAtUtc,
            delivery.NextAttemptAtUtc,
            delivery.ResponseCode,
            delivery.ResponseBody,
            delivery.CreatedAtUtc,
            delivery.UpdatedAtUtc,
            delivery.RowVersion);
    }
}
