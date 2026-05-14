using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class OrderStatusChangeViewModel
{
    public Guid OrderId { get; set; }

    [Required]
    public string ToStatus { get; set; } = "Processing";

    [StringLength(512)]
    public string? Comment { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<string> StatusOptions { get; set; } = [];
}
