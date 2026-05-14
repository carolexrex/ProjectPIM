using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Markets;

public sealed class CreateMarketRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; init; } = string.Empty;

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string DefaultCulture { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string VatMode { get; init; } = "Gross";
}

public sealed class UpdateMarketRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; init; } = string.Empty;

    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string DefaultCulture { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string VatMode { get; init; } = "Gross";

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AssignMarketCurrenciesRequest
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; init; } = string.Empty;

    [Required]
    public IReadOnlyList<string> Currencies { get; init; } = [];

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class AssignMarketCulturesRequest
{
    [Required]
    [StringLength(16, MinimumLength = 2)]
    public string DefaultCulture { get; init; } = string.Empty;

    [Required]
    public IReadOnlyList<string> Cultures { get; init; } = [];

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertMarketProductAssignmentRequest
{
    [Required]
    [StringLength(32)]
    public string Status { get; init; } = "Active";

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class RemoveMarketProductAssignmentRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
