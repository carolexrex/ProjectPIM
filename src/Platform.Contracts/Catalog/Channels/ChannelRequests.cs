using System.ComponentModel.DataAnnotations;
using Platform.Contracts.Common.Validation;

namespace Platform.Contracts.Catalog.Channels;

public sealed class CreateChannelRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [StringLength(256)]
    public string? HostName { get; init; }
}

public sealed class UpdateChannelRequest
{
    [Required]
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [StringLength(256)]
    public string? HostName { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpsertChannelMarketAssignmentRequest
{
    [NotEmptyGuid]
    public Guid MarketId { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class RemoveChannelMarketAssignmentRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
