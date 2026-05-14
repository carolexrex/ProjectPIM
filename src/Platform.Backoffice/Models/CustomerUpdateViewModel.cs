using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class CustomerUpdateViewModel : CustomerCreateViewModel
{
    public Guid Id { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
