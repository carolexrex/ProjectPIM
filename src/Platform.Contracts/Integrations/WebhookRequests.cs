using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Integrations;

public sealed class CreateWebhookSubscriptionRequest
{
    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(2048)]
    public string EndpointUrl { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Secret { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> EventTypes { get; init; } = [];

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateWebhookSubscriptionRequest
{
    [Required]
    [StringLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(2048)]
    public string EndpointUrl { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Secret { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> EventTypes { get; init; } = [];

    public bool IsActive { get; init; }

    [Required]
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class ReplayWebhookDeliveryRequest
{
    [Required]
    public string RowVersion { get; init; } = string.Empty;
}
