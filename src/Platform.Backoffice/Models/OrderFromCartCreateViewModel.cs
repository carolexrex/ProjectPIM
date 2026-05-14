using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class OrderFromCartCreateViewModel
{
    public Guid CartId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
