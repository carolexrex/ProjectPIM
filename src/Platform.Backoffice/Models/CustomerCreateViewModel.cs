using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public class CustomerCreateViewModel
{
    [StringLength(128)]
    public string? ExternalId { get; set; }

    [StringLength(128)]
    public string? UserId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Phone { get; set; }

    [StringLength(16)]
    public string? PreferredCulture { get; set; }

    public Guid? DefaultMarketId { get; set; }

    [Required]
    public string Status { get; set; } = "Active";

    public bool IsGuest { get; set; }
    public IReadOnlyList<string> StatusOptions { get; set; } = [];
    public IReadOnlyList<MarketLookupOptionViewModel> MarketOptions { get; set; } = [];
}
