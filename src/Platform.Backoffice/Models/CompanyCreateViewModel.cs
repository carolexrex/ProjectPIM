using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public class CompanyCreateViewModel
{
    [StringLength(128)]
    public string? ExternalId { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    public string? LegalName { get; set; }

    [StringLength(64)]
    public string? OrganizationNumber { get; set; }

    [StringLength(64)]
    public string? VatNumber { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(64)]
    public string? Phone { get; set; }

    public Guid? DefaultMarketId { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string? DefaultCurrency { get; set; }

    [Required]
    public string Status { get; set; } = "Active";

    public IReadOnlyList<string> StatusOptions { get; set; } = [];
    public IReadOnlyList<MarketLookupOptionViewModel> MarketOptions { get; set; } = [];
}
