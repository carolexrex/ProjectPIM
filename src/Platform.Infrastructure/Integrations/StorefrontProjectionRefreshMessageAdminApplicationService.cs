using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Integrations;
using Platform.Application.Integrations.Commands;
using Platform.Application.Integrations.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Integrations;

public sealed class StorefrontProjectionRefreshMessageAdminApplicationService : IStorefrontProjectionRefreshMessageAdminApplicationService
{
    private static readonly IReadOnlySet<string> SupportedStatusFilters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "open",
        StorefrontProjectionRefreshMessageStatuses.Pending,
        StorefrontProjectionRefreshMessageStatuses.Delayed,
        StorefrontProjectionRefreshMessageStatuses.Abandoned,
        StorefrontProjectionRefreshMessageStatuses.Published
    };

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StorefrontProjectionRefreshMessageAdminApplicationService(
        IOutboxMessageRepository outboxMessageRepository,
        IUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>> ListAsync(
        ListStorefrontProjectionRefreshMessagesQuery query,
        CancellationToken cancellationToken)
    {
        ValidateStatus(query.Status);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        var nowUtc = DateTime.UtcNow;
        var result = await _outboxMessageRepository.ListByEventTypeAsync(
            WebhookEventTypes.StorefrontProjectionRefreshRequested,
            query.Status,
            page,
            pageSize,
            query.Sort,
            nowUtc,
            cancellationToken);

        return new PagedResponse<StorefrontProjectionRefreshMessageSummaryDto>(
            result.Items.Select(message => MapSummary(message, nowUtc)).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<StorefrontProjectionRefreshMessageDetailsDto?> GetByIdAsync(
        GetStorefrontProjectionRefreshMessageByIdQuery query,
        CancellationToken cancellationToken)
    {
        var message = await GetRefreshMessageAsync(query.OutboxMessageId, cancellationToken);
        return message is null ? null : MapDetails(message, DateTime.UtcNow);
    }

    public async Task<StorefrontProjectionRefreshMessageDetailsDto?> ResetAsync(
        ResetStorefrontProjectionRefreshMessageCommand command,
        CancellationToken cancellationToken)
    {
        var message = await GetRefreshMessageAsync(command.OutboxMessageId, cancellationToken);
        if (message is null)
        {
            return null;
        }

        if (!message.IsProcessingAbandoned)
        {
            throw new RequestValidationException(nameof(command.OutboxMessageId), "Only abandoned storefront projection refresh messages can be reset.");
        }

        message.ResetProcessingForReplay(command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(message, DateTime.UtcNow);
    }

    private async Task<OutboxMessage?> GetRefreshMessageAsync(Guid outboxMessageId, CancellationToken cancellationToken)
    {
        var message = await _outboxMessageRepository.GetByIdAsync(outboxMessageId, cancellationToken);
        return message is not null
            && string.Equals(message.EventType, WebhookEventTypes.StorefrontProjectionRefreshRequested, StringComparison.Ordinal)
            ? message
            : null;
    }

    private static void ValidateStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || SupportedStatusFilters.Contains(status.Trim()))
        {
            return;
        }

        throw new RequestValidationException(nameof(status), "Status must be one of open, pending, delayed, abandoned, or published.");
    }

    private static StorefrontProjectionRefreshMessageSummaryDto MapSummary(OutboxMessage message, DateTime nowUtc)
    {
        return new StorefrontProjectionRefreshMessageSummaryDto(
            message.Id,
            message.EventType,
            message.AggregateType,
            message.AggregateId,
            MapStatus(message, nowUtc),
            message.ProcessingAttemptCount,
            message.LastProcessingError,
            message.NextProcessingAttemptAtUtc,
            message.ProcessingAbandonedAtUtc,
            message.PublishedAtUtc,
            message.OccurredAtUtc,
            message.CreatedAtUtc,
            message.UpdatedAtUtc,
            message.RowVersion);
    }

    private static StorefrontProjectionRefreshMessageDetailsDto MapDetails(OutboxMessage message, DateTime nowUtc)
    {
        return new StorefrontProjectionRefreshMessageDetailsDto(
            message.Id,
            message.EventType,
            message.AggregateType,
            message.AggregateId,
            MapStatus(message, nowUtc),
            message.PayloadJson,
            message.ProcessingAttemptCount,
            message.LastProcessingError,
            message.NextProcessingAttemptAtUtc,
            message.ProcessingAbandonedAtUtc,
            message.PublishedAtUtc,
            message.OccurredAtUtc,
            message.CreatedAtUtc,
            message.UpdatedAtUtc,
            message.RowVersion);
    }

    private static string MapStatus(OutboxMessage message, DateTime nowUtc)
    {
        if (message.PublishedAtUtc.HasValue)
        {
            return StorefrontProjectionRefreshMessageStatuses.Published;
        }

        if (message.ProcessingAbandonedAtUtc.HasValue)
        {
            return StorefrontProjectionRefreshMessageStatuses.Abandoned;
        }

        return message.NextProcessingAttemptAtUtc.HasValue && message.NextProcessingAttemptAtUtc > nowUtc
            ? StorefrontProjectionRefreshMessageStatuses.Delayed
            : StorefrontProjectionRefreshMessageStatuses.Pending;
    }
}
