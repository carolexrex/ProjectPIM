using System.Globalization;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Markets;
using Platform.Application.Storefront;
using Platform.Domain.Catalog.Inventory;

namespace Platform.Infrastructure.Catalog.Inventory;

public sealed class InventoryReservationService : IInventoryReservationService
{
    private readonly IInventoryLocationRepository _inventoryLocationRepository;
    private readonly IInventoryBalanceRepository _inventoryBalanceRepository;
    private readonly IInventoryAllocationRepository _inventoryAllocationRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IStorefrontProjectionRefreshRequestPublisher _storefrontProjectionRefreshRequestPublisher;
    private readonly TimeProvider _timeProvider;

    public InventoryReservationService(
        IInventoryLocationRepository inventoryLocationRepository,
        IInventoryBalanceRepository inventoryBalanceRepository,
        IInventoryAllocationRepository inventoryAllocationRepository,
        IMarketRepository marketRepository,
        IStorefrontProjectionRefreshRequestPublisher storefrontProjectionRefreshRequestPublisher,
        TimeProvider timeProvider)
    {
        _inventoryLocationRepository = inventoryLocationRepository;
        _inventoryBalanceRepository = inventoryBalanceRepository;
        _inventoryAllocationRepository = inventoryAllocationRepository;
        _marketRepository = marketRepository;
        _storefrontProjectionRefreshRequestPublisher = storefrontProjectionRefreshRequestPublisher;
        _timeProvider = timeProvider;
    }

    public async Task<InventoryReservationPlan> PlanReservationAsync(
        Guid marketId,
        IReadOnlyCollection<InventoryDemand> demand,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();
        var demandByVariant = demand
            .GroupBy(x => x.VariantId)
            .Select(group => (VariantId: group.Key, Sku: group.First().Sku, Quantity: group.Sum(x => x.Quantity)))
            .OrderBy(x => x.VariantId)
            .ToList();

        foreach (var line in demandByVariant.Where(x => !InventoryAllocationPolicy.HasSupportedScale(x.Quantity)))
        {
            AddError(
                errors,
                VariantKey(line.VariantId),
                $"Quantity {Format(line.Quantity)} for {line.Sku} has more than {InventoryAllocationPolicy.QuantityDecimalPlaces} decimal places.");
        }

        if (errors.Count > 0)
        {
            throw ToValidationException(errors);
        }

        var locations = await _inventoryLocationRepository.ListActiveByMarketAsync(marketId, cancellationToken);
        var priorityByLocation = locations.ToDictionary(
            x => x.Id,
            x => (Priority: x.MarketAssignments.First(y => y.MarketId == marketId).Priority, x.Code));

        var balances = await _inventoryBalanceRepository.ListForUpdateAsync(
            demandByVariant.Select(x => x.VariantId).ToList(),
            priorityByLocation.Keys.ToList(),
            cancellationToken);
        var balancesByVariant = balances.ToLookup(x => x.VariantId);

        var allocations = new List<PlannedInventoryAllocation>();
        string? marketCode = null;
        foreach (var line in demandByVariant)
        {
            var sources = balancesByVariant[line.VariantId]
                .Select(balance =>
                {
                    var location = priorityByLocation[balance.InventoryLocationId];
                    return new InventoryAllocationSource(balance, location.Priority, location.Code);
                });
            var outcome = InventoryAllocationPolicy.Allocate(line.Quantity, sources);
            if (outcome.IsSatisfied)
            {
                allocations.AddRange(outcome.Allocations);
                continue;
            }

            marketCode ??= (await _marketRepository.GetByIdAsync(marketId, cancellationToken))?.Code ?? marketId.ToString();
            AddError(
                errors,
                VariantKey(line.VariantId),
                outcome.HasSource
                    ? $"Insufficient stock for {line.Sku} in market '{marketCode}': requested {Format(line.Quantity)}, available {Format(outcome.AvailableQuantity)}."
                    : $"{line.Sku} is not stocked at any active inventory location assigned to market '{marketCode}'.");
        }

        if (errors.Count > 0)
        {
            throw ToValidationException(errors);
        }

        return new InventoryReservationPlan(allocations);
    }

    public async Task ApplyReservationAsync(Guid orderId, InventoryReservationPlan plan, CancellationToken cancellationToken)
    {
        if (plan.Allocations.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var planned in plan.Allocations)
        {
            var balance = planned.Balance;
            var transaction = balance.Reserve(planned.Quantity, orderId, now);
            await _inventoryBalanceRepository.AddTransactionAsync(transaction, cancellationToken);
            await _inventoryAllocationRepository.AddAsync(
                new InventoryAllocation(
                    Guid.NewGuid(),
                    orderId,
                    balance.Id,
                    balance.InventoryLocationId,
                    balance.VariantId,
                    planned.Quantity,
                    planned.BackorderedQuantity,
                    now),
                cancellationToken);
        }

        await _storefrontProjectionRefreshRequestPublisher.EnqueueVariantsRefreshAsync(
            plan.Allocations.Select(x => x.Balance.VariantId).Distinct().ToList(),
            "OrderInventoryReserved",
            cancellationToken);
    }

    public async Task<InventorySettlementPlan> PlanSettlementAsync(
        Guid orderId,
        InventorySettlementType type,
        CancellationToken cancellationToken)
    {
        // Orders placed before reservations existed have no allocations; settling them is a no-op
        // so no stock movement is invented for them.
        var outstanding = (await _inventoryAllocationRepository.ListByOrderAsync(orderId, cancellationToken))
            .Where(x => x.IsOutstanding)
            .ToList();
        if (outstanding.Count == 0)
        {
            return new InventorySettlementPlan(orderId, type, []);
        }

        var balances = (await _inventoryBalanceRepository.GetByIdsForUpdateAsync(
                outstanding.Select(x => x.InventoryBalanceId).Distinct().ToList(),
                cancellationToken))
            .ToDictionary(x => x.Id);

        var locationCodes = type == InventorySettlementType.Ship
            ? (await _inventoryLocationRepository.GetByIdsAsync(
                    balances.Values.Select(x => x.InventoryLocationId).Distinct().ToList(),
                    cancellationToken))
                .ToDictionary(x => x.Id, x => x.Code)
            : [];

        var errors = new Dictionary<string, List<string>>();
        foreach (var allocation in outstanding)
        {
            if (!balances.TryGetValue(allocation.InventoryBalanceId, out var balance))
            {
                AddError(errors, VariantKey(allocation.VariantId), $"Inventory balance {allocation.InventoryBalanceId} for allocation {allocation.Id} no longer exists.");
                continue;
            }

            if (allocation.Quantity > balance.ReservedQuantity)
            {
                AddError(
                    errors,
                    VariantKey(allocation.VariantId),
                    $"Balance {balance.Id} has only {Format(balance.ReservedQuantity)} reserved; the order holds {Format(allocation.Quantity)}.");
                continue;
            }

            if (type == InventorySettlementType.Ship && !balance.CanShipReservation(allocation.Quantity))
            {
                AddError(
                    errors,
                    VariantKey(allocation.VariantId),
                    $"Cannot complete the order: location '{locationCodes.GetValueOrDefault(balance.InventoryLocationId, balance.InventoryLocationId.ToString())}' has {Format(balance.OnHandQuantity)} on hand for variant {allocation.VariantId}, but {Format(allocation.Quantity)} must ship.");
            }
        }

        if (errors.Count > 0)
        {
            throw ToValidationException(errors);
        }

        return new InventorySettlementPlan(
            orderId,
            type,
            outstanding.Select(x => new InventorySettlementItem(x, balances[x.InventoryBalanceId])).ToList());
    }

    public async Task ApplySettlementAsync(InventorySettlementPlan plan, CancellationToken cancellationToken)
    {
        if (plan.Items.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var item in plan.Items)
        {
            InventoryTransaction transaction;
            if (plan.Type == InventorySettlementType.Ship)
            {
                transaction = item.Balance.ShipReservation(item.Allocation.Quantity, plan.OrderId, now);
                item.Allocation.MarkShipped(now);
            }
            else
            {
                transaction = item.Balance.ReleaseReservation(item.Allocation.Quantity, plan.OrderId, now);
                item.Allocation.MarkReleased(now);
            }

            await _inventoryBalanceRepository.AddTransactionAsync(transaction, cancellationToken);
        }

        // Shipping lowers on-hand and reserved stock equally, so available stock (all the storefront
        // projects) is unchanged; only a release makes stock available again.
        if (plan.Type == InventorySettlementType.Release)
        {
            await _storefrontProjectionRefreshRequestPublisher.EnqueueVariantsRefreshAsync(
                plan.Items.Select(x => x.Balance.VariantId).Distinct().ToList(),
                "OrderInventoryReleased",
                cancellationToken);
        }
    }

    private static string VariantKey(Guid variantId)
    {
        return $"VariantId.{variantId}";
    }

    private static string Format(decimal quantity)
    {
        return quantity.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private static RequestValidationException ToValidationException(Dictionary<string, List<string>> errors)
    {
        return new RequestValidationException(errors.ToDictionary(x => x.Key, x => x.Value.ToArray()));
    }
}
