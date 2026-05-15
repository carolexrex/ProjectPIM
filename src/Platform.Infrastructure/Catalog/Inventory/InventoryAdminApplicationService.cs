using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Inventory.Commands;
using Platform.Application.Catalog.Inventory.Queries;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Variants;
using Platform.Application.Storefront;
using Platform.Contracts.Catalog.Inventory;
using Platform.Contracts.Common;
using Platform.Domain.Catalog.Inventory;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class InventoryAdminApplicationService : IInventoryAdminApplicationService
{
    private static readonly string[] AllowedLocationTypes = ["Warehouse", "Store", "Supplier", "External"];
    private static readonly string[] AllowedTransactionTypes = ["Adjustment"];

    private readonly IInventoryLocationRepository _inventoryLocationRepository;
    private readonly IInventoryBalanceRepository _inventoryBalanceRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IStorefrontProjectionRefreshRequestPublisher _storefrontProjectionRefreshRequestPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryAdminApplicationService(
        IInventoryLocationRepository inventoryLocationRepository,
        IInventoryBalanceRepository inventoryBalanceRepository,
        IMarketRepository marketRepository,
        IVariantRepository variantRepository,
        IStorefrontProjectionRefreshRequestPublisher storefrontProjectionRefreshRequestPublisher,
        IUnitOfWork unitOfWork)
    {
        _inventoryLocationRepository = inventoryLocationRepository;
        _inventoryBalanceRepository = inventoryBalanceRepository;
        _marketRepository = marketRepository;
        _variantRepository = variantRepository;
        _storefrontProjectionRefreshRequestPublisher = storefrontProjectionRefreshRequestPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<InventoryLocationSummaryDto>> ListLocationsAsync(ListInventoryLocationsQuery query, CancellationToken cancellationToken)
    {
        var result = await _inventoryLocationRepository.ListAsync(query, cancellationToken);
        var counts = await _inventoryBalanceRepository.CountByInventoryLocationIdsAsync(result.Items.Select(x => x.Id).ToList(), cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<InventoryLocationSummaryDto>(
            result.Items.Select(location => new InventoryLocationSummaryDto(
                location.Id,
                location.Code,
                location.Name,
                location.Type,
                location.Status,
                location.MarketAssignments.Count,
                counts.GetValueOrDefault(location.Id),
                location.UpdatedAtUtc,
                location.RowVersion)).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<InventoryLocationDetailsDto?> GetLocationByIdAsync(GetInventoryLocationByIdQuery query, CancellationToken cancellationToken)
    {
        var location = await _inventoryLocationRepository.GetByIdAsync(query.InventoryLocationId, cancellationToken);
        return location is null ? null : await MapLocationDetailsAsync(location, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto> CreateLocationAsync(CreateInventoryLocationCommand command, CancellationToken cancellationToken)
    {
        await EnsureCodeUniqueAsync(command.Code, null, cancellationToken);
        ValidateLocationType(command.Type);

        var now = DateTime.UtcNow;
        var location = new InventoryLocation(Guid.NewGuid(), command.Code, command.Name, command.Type, command.CountryCode, now, now);
        await _inventoryLocationRepository.AddAsync(location, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapLocationDetailsAsync(location, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> UpdateLocationAsync(UpdateInventoryLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await _inventoryLocationRepository.GetByIdAsync(command.InventoryLocationId, cancellationToken);
        if (location is null)
        {
            return null;
        }

        await EnsureCodeUniqueAsync(command.Code, command.InventoryLocationId, cancellationToken);
        ValidateLocationType(command.Type);
        location.Update(command.Code, command.Name, command.Type, command.CountryCode, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapLocationDetailsAsync(location, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> ArchiveLocationAsync(ArchiveInventoryLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await _inventoryLocationRepository.GetByIdAsync(command.InventoryLocationId, cancellationToken);
        if (location is null)
        {
            return null;
        }

        location.Archive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapLocationDetailsAsync(location, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> UpsertLocationMarketAssignmentAsync(UpsertInventoryLocationMarketAssignmentCommand command, CancellationToken cancellationToken)
    {
        var location = await _inventoryLocationRepository.GetByIdAsync(command.InventoryLocationId, cancellationToken);
        if (location is null)
        {
            return null;
        }

        if (await _marketRepository.GetByIdAsync(command.MarketId, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(UpsertInventoryLocationMarketAssignmentCommand.MarketId), "Unknown market.");
        }

        location.UpsertMarketAssignment(command.MarketId, command.Priority, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapLocationDetailsAsync(location, cancellationToken);
    }

    public async Task<InventoryLocationDetailsDto?> RemoveLocationMarketAssignmentAsync(RemoveInventoryLocationMarketAssignmentCommand command, CancellationToken cancellationToken)
    {
        var location = await _inventoryLocationRepository.GetByIdAsync(command.InventoryLocationId, cancellationToken);
        if (location is null)
        {
            return null;
        }

        location.RemoveMarketAssignment(command.MarketId, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapLocationDetailsAsync(location, cancellationToken);
    }

    public async Task<InventoryBalanceDto> UpsertBalanceAsync(UpsertInventoryBalanceCommand command, CancellationToken cancellationToken)
    {
        ValidateSnapshotQuantities(command.OnHandQuantity, command.ReservedQuantity, command.IncomingQuantity, command.Backorderable);

        var location = await _inventoryLocationRepository.GetByIdAsync(command.InventoryLocationId, cancellationToken);
        if (location is null)
        {
            throw new RequestValidationException(nameof(UpsertInventoryBalanceCommand.InventoryLocationId), "Unknown inventory location.");
        }

        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            throw new RequestValidationException(nameof(UpsertInventoryBalanceCommand.VariantId), "Unknown variant.");
        }

        var balance = await _inventoryBalanceRepository.GetByInventoryLocationAndVariantAsync(command.InventoryLocationId, command.VariantId, cancellationToken);
        if (balance is null)
        {
            balance = new InventoryBalance(
                Guid.NewGuid(),
                command.InventoryLocationId,
                command.VariantId,
                command.OnHandQuantity,
                command.ReservedQuantity,
                command.IncomingQuantity,
                command.Backorderable,
                DateTime.UtcNow);
            await _inventoryBalanceRepository.AddAsync(balance, cancellationToken);
        }
        else
        {
            balance.UpdateSnapshot(
                command.OnHandQuantity,
                command.ReservedQuantity,
                command.IncomingQuantity,
                command.Backorderable,
                command.RowVersion);
        }

        await _storefrontProjectionRefreshRequestPublisher.EnqueueVariantRefreshAsync(
            variant.Id,
            "InventoryBalanceUpserted",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapBalance(balance, variant.Sku);
    }

    public async Task<InventoryTransactionDto> AdjustInventoryAsync(AdjustInventoryCommand command, CancellationToken cancellationToken)
    {
        ValidateTransactionType(command.Type);

        var location = await _inventoryLocationRepository.GetByIdAsync(command.InventoryLocationId, cancellationToken);
        if (location is null)
        {
            throw new RequestValidationException(nameof(AdjustInventoryCommand.InventoryLocationId), "Unknown inventory location.");
        }

        var variant = await _variantRepository.GetByIdAsync(command.VariantId, cancellationToken);
        if (variant is null)
        {
            throw new RequestValidationException(nameof(AdjustInventoryCommand.VariantId), "Unknown variant.");
        }

        var balance = await _inventoryBalanceRepository.GetByInventoryLocationAndVariantAsync(command.InventoryLocationId, command.VariantId, cancellationToken);
        if (balance is null)
        {
            balance = new InventoryBalance(
                Guid.NewGuid(),
                command.InventoryLocationId,
                command.VariantId,
                0m,
                0m,
                0m,
                false,
                DateTime.UtcNow);
            await _inventoryBalanceRepository.AddAsync(balance, cancellationToken);
        }

        var referenceType = string.IsNullOrWhiteSpace(command.ReferenceType) ? "ManualAdjustment" : command.ReferenceType.Trim();
        var referenceId = command.ReferenceId ?? Guid.NewGuid();
        var transaction = balance.Adjust(command.Type, command.QuantityDelta, referenceType, referenceId, DateTime.UtcNow);

        if (!balance.Backorderable && balance.OnHandQuantity < 0)
        {
            throw new RequestValidationException(nameof(AdjustInventoryCommand.QuantityDelta), "Adjustment would make on-hand quantity negative for a non-backorderable balance.");
        }

        await _inventoryBalanceRepository.AddTransactionAsync(transaction, cancellationToken);
        await _storefrontProjectionRefreshRequestPublisher.EnqueueVariantRefreshAsync(
            variant.Id,
            "InventoryAdjusted",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTransaction(transaction, variant.Sku);
    }

    public async Task<VariantInventorySnapshotDto?> GetVariantInventorySnapshotAsync(GetVariantInventorySnapshotQuery query, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdAsync(query.VariantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        var balances = await _inventoryBalanceRepository.ListByVariantAsync(query.VariantId, cancellationToken);
        var locations = await _inventoryLocationRepository.GetByIdsAsync(balances.Select(x => x.InventoryLocationId).Distinct().ToList(), cancellationToken);
        var locationMap = locations.ToDictionary(x => x.Id);

        var snapshotLocations = balances
            .OrderBy(x => locationMap.GetValueOrDefault(x.InventoryLocationId)?.Code)
            .Select(balance =>
            {
                var location = locationMap.GetValueOrDefault(balance.InventoryLocationId);
                return new VariantInventoryLocationDto(
                    balance.InventoryLocationId,
                    location?.Code ?? balance.InventoryLocationId.ToString(),
                    balance.OnHandQuantity,
                    balance.ReservedQuantity,
                    balance.IncomingQuantity,
                    balance.AvailableQuantity,
                    balance.Backorderable,
                    balance.UpdatedAtUtc);
            })
            .ToList();

        return new VariantInventorySnapshotDto(
            variant.Id,
            variant.Sku,
            snapshotLocations,
            snapshotLocations.Sum(x => x.OnHandQuantity),
            snapshotLocations.Sum(x => x.AvailableQuantity));
    }

    private async Task EnsureCodeUniqueAsync(string code, Guid? currentLocationId, CancellationToken cancellationToken)
    {
        var existing = await _inventoryLocationRepository.GetByCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken);
        if (existing is not null && existing.Id != currentLocationId)
        {
            throw new ConflictException("Inventory location code already exists.");
        }
    }

    private static void ValidateLocationType(string type)
    {
        if (!AllowedLocationTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(CreateInventoryLocationCommand.Type), "Unknown inventory location type.");
        }
    }

    private static void ValidateTransactionType(string type)
    {
        if (!AllowedTransactionTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(AdjustInventoryCommand.Type), "Unknown inventory transaction type.");
        }
    }

    private static void ValidateSnapshotQuantities(decimal onHand, decimal reserved, decimal incoming, bool backorderable)
    {
        if (reserved < 0m)
        {
            throw new RequestValidationException(nameof(UpsertInventoryBalanceCommand.ReservedQuantity), "Reserved quantity cannot be negative.");
        }

        if (incoming < 0m)
        {
            throw new RequestValidationException(nameof(UpsertInventoryBalanceCommand.IncomingQuantity), "Incoming quantity cannot be negative.");
        }

        if (!backorderable && onHand < 0m)
        {
            throw new RequestValidationException(nameof(UpsertInventoryBalanceCommand.OnHandQuantity), "On-hand quantity cannot be negative for a non-backorderable balance.");
        }
    }

    private async Task<InventoryLocationDetailsDto> MapLocationDetailsAsync(InventoryLocation location, CancellationToken cancellationToken)
    {
        var markets = await _marketRepository.GetByIdsAsync(location.MarketAssignments.Select(x => x.MarketId).Distinct().ToList(), cancellationToken);
        var marketMap = markets.ToDictionary(x => x.Id);

        var balances = await _inventoryBalanceRepository.ListByInventoryLocationAsync(location.Id, cancellationToken);
        var variants = await _variantRepository.GetByIdsAsync(balances.Select(x => x.VariantId).Distinct().ToList(), cancellationToken);
        var variantMap = variants.ToDictionary(x => x.Id);

        var transactions = await _inventoryBalanceRepository.ListTransactionsByInventoryLocationAsync(location.Id, 20, cancellationToken);

        return new InventoryLocationDetailsDto(
            location.Id,
            location.Code,
            location.Name,
            location.Type,
            location.CountryCode,
            location.Status,
            location.MarketAssignments.Select(assignment =>
            {
                var market = marketMap.GetValueOrDefault(assignment.MarketId);
                return new InventoryLocationMarketAssignmentDto(
                    assignment.MarketId,
                    market?.Code ?? assignment.MarketId.ToString(),
                    market?.Name ?? assignment.MarketId.ToString(),
                    assignment.Priority);
            }).ToList(),
            balances
                .Select(balance => MapBalance(balance, variantMap.GetValueOrDefault(balance.VariantId)?.Sku ?? balance.VariantId.ToString()))
                .OrderBy(x => x.VariantSku)
                .ToList(),
            transactions
                .Select(transaction => MapTransaction(transaction, variantMap.GetValueOrDefault(transaction.VariantId)?.Sku ?? transaction.VariantId.ToString()))
                .ToList(),
            location.CreatedAtUtc,
            location.UpdatedAtUtc,
            location.RowVersion);
    }

    private static InventoryBalanceDto MapBalance(InventoryBalance balance, string variantSku)
    {
        return new InventoryBalanceDto(
            balance.Id,
            balance.InventoryLocationId,
            balance.VariantId,
            variantSku,
            balance.OnHandQuantity,
            balance.ReservedQuantity,
            balance.IncomingQuantity,
            balance.AvailableQuantity,
            balance.Backorderable,
            balance.UpdatedAtUtc,
            balance.RowVersion);
    }

    private static InventoryTransactionDto MapTransaction(InventoryTransaction transaction, string variantSku)
    {
        return new InventoryTransactionDto(
            transaction.Id,
            transaction.InventoryLocationId,
            transaction.VariantId,
            variantSku,
            transaction.Type,
            transaction.QuantityDelta,
            transaction.ReferenceType,
            transaction.ReferenceId,
            transaction.OccurredAtUtc);
    }
}
