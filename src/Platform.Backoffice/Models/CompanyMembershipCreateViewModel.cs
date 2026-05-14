using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class CompanyMembershipCreateViewModel
{
    public Guid CompanyId { get; set; }
    public Guid? CustomerId { get; set; }

    [Required]
    public string Role { get; set; } = "Buyer";

    [Required]
    public string Status { get; set; } = "Active";

    public bool IsDefaultCompany { get; set; }
    public bool CanPlaceOrders { get; set; } = true;
    public bool CanApproveOrders { get; set; }
    public bool CanManageUsers { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public IReadOnlyList<string> RoleOptions { get; set; } = [];
    public IReadOnlyList<string> StatusOptions { get; set; } = [];
    public IReadOnlyList<CustomerLookupOptionViewModel> CustomerOptions { get; set; } = [];
}
