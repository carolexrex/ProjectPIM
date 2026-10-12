using Platform.Domain.Catalog.Inventory;

namespace Platform.Application.Catalog.Inventory;

/// <summary>
/// Order inventory lifecycle: reserve on placement, release on cancellation, ship on completion.
/// Each step is split into a side-effect-free plan, which validates everything and throws
/// <see cref="Errors.RequestValidationException"/> on failure, and an apply step that cannot fail
/// for a valid plan. Callers plan before their first mutation so a rejected order leaves no
/// partial changes. Apply stages changes only; the caller's unit of work commits them.
/// </summary>
public interface IInventoryReservationService
{
    Task<InventoryReservationPlan> PlanReservationAsync(
        Guid marketId,
        IReadOnlyCollection<InventoryDemand> demand,
        CancellationToken cancellationToken);

    Task ApplyReservationAsync(Guid orderId, InventoryReservationPlan plan, CancellationToken cancellationToken);

    Task<InventorySettlementPlan> PlanSettlementAsync(
        Guid orderId,
        InventorySettlementType type,
        CancellationToken cancellationToken);

    Task ApplySettlementAsync(InventorySettlementPlan plan, CancellationToken cancellationToken);
}

/// <summary>One order line's demand; lines for the same variant are aggregated by the planner.</summary>
public sealed record InventoryDemand(Guid VariantId, string Sku, decimal Quantity);

public sealed record InventoryReservationPlan(IReadOnlyList<PlannedInventoryAllocation> Allocations);

public enum InventorySettlementType
{
    Release = 0,
    Ship = 1
}

public sealed record InventorySettlementPlan(
    Guid OrderId,
    InventorySettlementType Type,
    IReadOnlyList<InventorySettlementItem> Items);

public sealed record InventorySettlementItem(InventoryAllocation Allocation, InventoryBalance Balance);
