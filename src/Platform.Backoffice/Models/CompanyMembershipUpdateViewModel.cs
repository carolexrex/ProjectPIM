using System.ComponentModel.DataAnnotations;

namespace Platform.Backoffice.Models;

public sealed class CompanyMembershipUpdateViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerDisplayName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Buyer";

    [Required]
    public string Status { get; set; } = "Active";

    public bool IsDefaultCompany { get; set; }
    public bool CanPlaceOrders { get; set; }
    public bool CanApproveOrders { get; set; }
    public bool CanManageUsers { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<string> RoleOptions { get; set; } = [];
    public IReadOnlyList<string> StatusOptions { get; set; } = [];
}
