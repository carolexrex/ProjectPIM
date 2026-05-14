namespace Platform.Infrastructure.Integrations;

public sealed class WebhookReplayOptions
{
    public const string SectionName = "Webhooks";

    public bool ManualReplayEnabled { get; init; }
    public int ManualReplayDelaySeconds { get; init; } = 300;
}
