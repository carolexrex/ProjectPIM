using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class OrderPaymentTransactionCreateViewModel
{
    public Guid OrderId { get; set; }

    [Required]
    [StringLength(64)]
    public string Provider { get; set; } = "Manual";

    [Required]
    [StringLength(128)]
    public string ProviderReference { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string Type { get; set; } = "Capture";

    [Required]
    public string Status { get; set; } = "Paid";

    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; set; } = "SEK";

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<string> TypeOptions { get; set; } = [];
    public IReadOnlyList<string> StatusOptions { get; set; } = [];
}
