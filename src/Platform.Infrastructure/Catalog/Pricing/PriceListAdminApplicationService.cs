using System.Text.Json;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Pricing.Commands;
using Platform.Application.Catalog.Pricing.Queries;
using Platform.Application.Catalog.Variants;
using Platform.Application.Integrations;
using Platform.Contracts.Catalog.Pricing;
using Platform.Contracts.Common;
using Platform.Contracts.Integrations;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Catalog.Pricing;
using Platform.Domain.Catalog.Variants;
using Platform.Domain.Integrations;

namespace Platform.Infrastructure.Catalog.Pricing;

public sealed class PriceListAdminApplicationService : IPriceListAdminApplicationService
{
    private readonly IPriceListRepository _priceListRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IOutboxEventPublisher _outboxEventPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public PriceListAdminApplicationService(
        IPriceListRepository priceListRepository,
        IMarketRepository marketRepository,
        IVariantRepository variantRepository,
        IOutboxEventPublisher outboxEventPublisher,
        IUnitOfWork unitOfWork)
    {
        _priceListRepository = priceListRepository;
        _marketRepository = marketRepository;
        _variantRepository = variantRepository;
        _outboxEventPublisher = outboxEventPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<PriceListSummaryDto>> ListAsync(ListPriceListsQuery query, CancellationToken cancellationToken)
    {
        var result = await _priceListRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<PriceListSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<PriceListDetailsDto?> GetByIdAsync(GetPriceListByIdQuery query, CancellationToken cancellationToken)
    {
        var priceList = await _priceListRepository.GetByIdAsync(query.PriceListId, cancellationToken);
        return priceList is null ? null : await MapDetailsAsync(priceList, cancellationToken);
    }

    public async Task<PriceListDetailsDto> CreateAsync(CreatePriceListCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeUniqueAsync(command.Code, null, cancellationToken);
        var now = DateTime.UtcNow;
        var priceList = new PriceList(
            Guid.NewGuid(),
            command.Code,
            command.Name,
            command.CurrencyCode,
            command.VatIncluded,
            command.ValidFromUtc,
            command.ValidToUtc,
            now,
            now);

        await _priceListRepository.AddAsync(priceList, cancellationToken);
        var details = await MapDetailsAsync(priceList, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.PriceListCreated, "Created", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<PriceListDetailsDto?> UpdateAsync(UpdatePriceListCommand command, CancellationToken cancellationToken)
    {
        var priceList = await _priceListRepository.GetByIdAsync(command.PriceListId, cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        await EnsureCodeUniqueAsync(command.Code, command.PriceListId, cancellationToken);
        priceList.Update(
            command.Code,
            command.Name,
            command.CurrencyCode,
            command.VatIncluded,
            command.ValidFromUtc,
            command.ValidToUtc,
            command.RowVersion);
        var details = await MapDetailsAsync(priceList, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.PriceListUpdated, "Updated", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<PriceListDetailsDto?> ArchiveAsync(ArchivePriceListCommand command, CancellationToken cancellationToken)
    {
        var priceList = await _priceListRepository.GetByIdAsync(command.PriceListId, cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        priceList.Archive();
        var details = await MapDetailsAsync(priceList, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.PriceListUpdated, "Archived", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<PriceListDetailsDto?> UpsertMarketAssignmentAsync(UpsertPriceListMarketAssignmentCommand command, CancellationToken cancellationToken)
    {
        var priceList = await _priceListRepository.GetByIdAsync(command.PriceListId, cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        var market = await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken);
        if (market is null)
        {
            throw new RequestValidationException(nameof(UpsertPriceListMarketAssignmentCommand.MarketId), "Unknown market.");
        }

        if (!market.Currencies.Any(x => string.Equals(x.CurrencyCode, priceList.CurrencyCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new RequestValidationException(nameof(UpsertPriceListMarketAssignmentCommand.MarketId), "Price list currency is not enabled for the selected market.");
        }

        priceList.UpsertMarketAssignment(command.MarketId, command.Priority, command.IsBasePriceList, command.RowVersion);
        var details = await MapDetailsAsync(priceList, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.PriceListUpdated, "MarketAssignmentUpserted", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<PriceListDetailsDto?> RemoveMarketAssignmentAsync(RemovePriceListMarketAssignmentCommand command, CancellationToken cancellationToken)
    {
        var priceList = await _priceListRepository.GetByIdAsync(command.PriceListId, cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        priceList.RemoveMarketAssignment(command.MarketId, command.RowVersion);
        var details = await MapDetailsAsync(priceList, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.PriceListUpdated, "MarketAssignmentRemoved", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<PriceListDetailsDto?> UpsertEntryAsync(UpsertPriceListEntryCommand command, CancellationToken cancellationToken)
    {
        var priceList = await _priceListRepository.GetByIdAsync(command.PriceListId, cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        if (!string.Equals(command.TargetType, "Variant", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(UpsertPriceListEntryCommand.TargetType), "Only variant pricing is supported in v1.");
        }

        ValidateEntryAmounts(command.Amount, command.CompareAtAmount);

        if (await _variantRepository.GetByIdAsync(command.TargetId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(UpsertPriceListEntryCommand.TargetId), "Unknown variant.");
        }

        priceList.UpsertEntry(
            command.EntryId,
            command.TargetType,
            command.TargetId,
            command.MinQuantity,
            command.Amount,
            command.CompareAtAmount,
            command.ValidFromUtc,
            command.ValidToUtc,
            command.RowVersion);
        var details = await MapDetailsAsync(priceList, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.PriceListUpdated, "EntryUpserted", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<PriceListDetailsDto?> RemoveEntryAsync(RemovePriceListEntryCommand command, CancellationToken cancellationToken)
    {
        var priceList = await _priceListRepository.GetByIdAsync(command.PriceListId, cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        priceList.RemoveEntry(command.EntryId, command.RowVersion);
        var details = await MapDetailsAsync(priceList, cancellationToken);
        await PublishEventAsync(WebhookEventTypes.PriceListUpdated, "EntryRemoved", details, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return details;
    }

    private Task PublishEventAsync(
        string eventType,
        string changeType,
        PriceListDetailsDto details,
        CancellationToken cancellationToken)
    {
        var payload = new PriceListWebhookEventDto(DateTime.UtcNow, changeType, details);
        return _outboxEventPublisher.EnqueueAsync(
            eventType,
            "PriceList",
            details.Id,
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? currentPriceListId, CancellationToken cancellationToken)
    {
        var existing = await _priceListRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != currentPriceListId)
        {
            throw new ConflictException("Price list code already exists.");
        }
    }

    private static void ValidateEntryAmounts(decimal amount, decimal? compareAtAmount)
    {
        if (amount <= 0)
        {
            throw new RequestValidationException(nameof(UpsertPriceListEntryCommand.Amount), "Amount must be greater than zero.");
        }

        if (compareAtAmount is decimal compareAt && compareAt < 0)
        {
            throw new RequestValidationException(nameof(UpsertPriceListEntryCommand.CompareAtAmount), "Compare-at amount cannot be negative.");
        }
    }

    private static PriceListSummaryDto MapSummary(PriceList priceList)
    {
        return new PriceListSummaryDto(
            priceList.Id,
            priceList.Code,
            priceList.Name,
            priceList.CurrencyCode,
            priceList.VatIncluded,
            priceList.Status,
            priceList.MarketAssignments.Count,
            priceList.Entries.Count,
            priceList.UpdatedAtUtc,
            priceList.RowVersion);
    }

    private async Task<PriceListDetailsDto> MapDetailsAsync(PriceList priceList, CancellationToken cancellationToken)
    {
        var marketsTask = _marketRepository.GetByIdsAsync(priceList.MarketAssignments.Select(x => x.MarketId).Distinct().ToList(), cancellationToken);
        var variantsTask = _variantRepository.GetByIdsAsync(
            priceList.Entries
                .Where(x => string.Equals(x.TargetType, "Variant", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.TargetId)
                .Distinct()
                .ToList(),
            cancellationToken);

        await Task.WhenAll(marketsTask, variantsTask);

        var marketMap = (await marketsTask).ToDictionary(x => x.Id);
        var variantMap = (await variantsTask).ToDictionary(x => x.Id);

        return new PriceListDetailsDto(
            priceList.Id,
            priceList.Code,
            priceList.Name,
            priceList.CurrencyCode,
            priceList.VatIncluded,
            priceList.ValidFromUtc,
            priceList.ValidToUtc,
            priceList.Status,
            priceList.MarketAssignments
                .Select(x => MapMarketAssignment(x, marketMap))
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.MarketCode)
                .ToList(),
            priceList.Entries
                .Select(x => MapEntry(x, variantMap))
                .OrderBy(x => x.TargetLabel)
                .ThenBy(x => x.MinQuantity)
                .ToList(),
            priceList.CreatedAtUtc,
            priceList.UpdatedAtUtc,
            priceList.RowVersion);
    }

    private static PriceListMarketAssignmentDto MapMarketAssignment(
        PriceListMarketAssignment assignment,
        IReadOnlyDictionary<Guid, Market> markets)
    {
        var market = markets.GetValueOrDefault(assignment.MarketId);
        return new PriceListMarketAssignmentDto(
            assignment.MarketId,
            market?.Code ?? assignment.MarketId.ToString(),
            market?.Name ?? assignment.MarketId.ToString(),
            assignment.Priority,
            assignment.IsBasePriceList);
    }

    private static PriceListEntryDto MapEntry(PriceListEntry entry, IReadOnlyDictionary<Guid, Variant> variants)
    {
        var label = entry.TargetType;
        if (string.Equals(entry.TargetType, "Variant", StringComparison.OrdinalIgnoreCase) && variants.TryGetValue(entry.TargetId, out var variant))
        {
            label = variant.Sku;
        }

        return new PriceListEntryDto(
            entry.Id,
            entry.TargetType,
            entry.TargetId,
            label,
            entry.MinQuantity,
            entry.Amount,
            entry.CompareAtAmount,
            entry.ValidFromUtc,
            entry.ValidToUtc);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
