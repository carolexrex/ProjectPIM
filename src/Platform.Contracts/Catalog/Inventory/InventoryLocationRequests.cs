using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Inventory;

public sealed class CreateInventoryLocationRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Type { get; init; } = string.Empty;

    [StringLength(2, MinimumLength = 2)]
    public string? CountryCode { get; init; }
}

public sealed class UpdateInventoryLocationRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Type { get; init; } = string.Empty;

    [StringLength(2, MinimumLength = 2)]
    public string? CountryCode { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertInventoryLocationMarketAssignmentRequest
{
    [NotEmptyGuid]
    public Guid MarketId { get; init; }

    [Range(0, int.MaxValue)]
    public int Priority { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class RemoveInventoryLocationMarketAssignmentRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertInventoryBalanceRequest
{
    [NotEmptyGuid]
    public Guid InventoryLocationId { get; init; }

    [NotEmptyGuid]
    public Guid VariantId { get; init; }

    public decimal OnHandQuantity { get; init; }
    public decimal ReservedQuantity { get; init; }
    public decimal IncomingQuantity { get; init; }
    public bool Backorderable { get; init; }
    public string? RowVersion { get; init; }
}

public sealed class AdjustInventoryRequest
{
    [NotEmptyGuid]
    public Guid InventoryLocationId { get; init; }

    [NotEmptyGuid]
    public Guid VariantId { get; init; }

    [Required]
    [StringLength(32)]
    public string Type { get; init; } = "Adjustment";

    public decimal QuantityDelta { get; init; }

    [Required]
    [StringLength(32)]
    public string ReferenceType { get; init; } = "ManualAdjustment";

    public Guid? ReferenceId { get; init; }
}
