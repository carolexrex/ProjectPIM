using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Cart;

public sealed class RepriceCartRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class ExpireCartRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
