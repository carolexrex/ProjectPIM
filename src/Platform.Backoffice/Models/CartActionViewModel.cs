using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class CartActionViewModel
{
    public Guid CartId { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
