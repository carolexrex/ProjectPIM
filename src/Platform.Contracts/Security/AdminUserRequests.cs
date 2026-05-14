using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Security;

public sealed class CreateAdminUserRequest
{
    [Required]
    [StringLength(64)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Status { get; init; } = "Active";

    [MinLength(1)]
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class UpdateAdminUserRequest
{
    [Required]
    [StringLength(128)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Status { get; init; } = "Active";

    [MinLength(1)]
    public IReadOnlyList<string> Roles { get; init; } = [];

    [StringLength(256, MinimumLength = 8)]
    public string? Password { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
