namespace Platform.Contracts.Catalog.Inventory;

public sealed record InventoryLocationMarketAssignmentDto(
    Guid MarketId,
    string MarketCode,
    string MarketName,
    int Priority);

public sealed record InventoryBalanceDto(
    Guid Id,
    Guid InventoryLocationId,
    Guid VariantId,
    string VariantSku,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal IncomingQuantity,
    decimal AvailableQuantity,
    bool Backorderable,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record InventoryTransactionDto(
    Guid Id,
    Guid InventoryLocationId,
    Guid VariantId,
    string VariantSku,
    string Type,
    decimal QuantityDelta,
    string ReferenceType,
    Guid ReferenceId,
    DateTime OccurredAtUtc);

public sealed record InventoryLocationSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string Status,
    int MarketCount,
    int BalanceCount,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record InventoryLocationDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string? CountryCode,
    string Status,
    IReadOnlyList<InventoryLocationMarketAssignmentDto> Markets,
    IReadOnlyList<InventoryBalanceDto> Balances,
    IReadOnlyList<InventoryTransactionDto> RecentTransactions,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string RowVersion);

public sealed record VariantInventoryLocationDto(
    Guid InventoryLocationId,
    string Code,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal IncomingQuantity,
    decimal AvailableQuantity,
    bool Backorderable,
    DateTime UpdatedAtUtc);

public sealed record VariantInventorySnapshotDto(
    Guid VariantId,
    string VariantSku,
    IReadOnlyList<VariantInventoryLocationDto> Locations,
    decimal TotalOnHandQuantity,
    decimal TotalAvailableQuantity);
