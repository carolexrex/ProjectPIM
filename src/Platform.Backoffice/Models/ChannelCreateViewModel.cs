using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class ChannelCreateViewModel
{
    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    public string? HostName { get; set; }
}
