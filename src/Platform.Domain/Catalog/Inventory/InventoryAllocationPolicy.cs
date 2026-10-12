namespace Platform.Domain.Catalog.Inventory;

/// <summary>
/// A balance that may supply stock to an order in one market, with the location's market priority.
/// </summary>
public sealed record InventoryAllocationSource(InventoryBalance Balance, int Priority, string LocationCode);

public sealed record PlannedInventoryAllocation(InventoryBalance Balance, decimal Quantity, decimal BackorderedQuantity);

public sealed record InventoryAllocationOutcome(
    IReadOnlyList<PlannedInventoryAllocation> Allocations,
    bool HasSource,
    decimal AvailableQuantity)
{
    public bool IsSatisfied => Allocations.Count > 0;
}

/// <summary>
/// Deterministic allocation of one variant's demand across the balances eligible in a market.
/// </summary>
public static class InventoryAllocationPolicy
{
    public const int QuantityDecimalPlaces = 4;

    /// <summary>
    /// Orders sources by market priority (lowest first), then location code, then balance id.
    /// Available stock is taken in that order. Any remainder is backordered on the first backorderable
    /// source; with no backorderable source the demand is not satisfied and nothing is allocated.
    /// At most one allocation is produced per balance.
    /// </summary>
    public static InventoryAllocationOutcome Allocate(decimal quantity, IEnumerable<InventoryAllocationSource> sources)
    {
        if (quantity <= 0m)
        {
            throw new InvalidOperationException("Demand quantity must be positive.");
        }

        var ordered = sources
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.LocationCode, StringComparer.Ordinal)
            .ThenBy(x => x.Balance.Id)
            .ToList();

        var available = ordered.Sum(x => Math.Max(x.Balance.AvailableQuantity, 0m));
        if (ordered.Count == 0)
        {
            return new InventoryAllocationOutcome([], false, 0m);
        }

        var taken = new Dictionary<Guid, decimal>();
        var remaining = quantity;
        foreach (var source in ordered)
        {
            if (remaining == 0m)
            {
                break;
            }

            var take = Math.Min(remaining, Math.Max(source.Balance.AvailableQuantity, 0m));
            if (take > 0m)
            {
                taken[source.Balance.Id] = take;
                remaining -= take;
            }
        }

        var backorderBalanceId = (Guid?)null;
        if (remaining > 0m)
        {
            var backorderSource = ordered.FirstOrDefault(x => x.Balance.Backorderable);
            if (backorderSource is null)
            {
                return new InventoryAllocationOutcome([], true, available);
            }

            backorderBalanceId = backorderSource.Balance.Id;
            taken[backorderSource.Balance.Id] = taken.GetValueOrDefault(backorderSource.Balance.Id) + remaining;
        }

        var allocations = ordered
            .Where(x => taken.ContainsKey(x.Balance.Id))
            .Select(x => new PlannedInventoryAllocation(
                x.Balance,
                taken[x.Balance.Id],
                x.Balance.Id == backorderBalanceId ? remaining : 0m))
            .ToList();

        return new InventoryAllocationOutcome(allocations, true, available);
    }

    public static bool HasSupportedScale(decimal quantity)
    {
        return decimal.Round(quantity, QuantityDecimalPlaces) == quantity;
    }
}
