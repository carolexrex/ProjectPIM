namespace Platform.Worker.IntegrationJobs;

public sealed class IntegrationJobWorkerOptions
{
    public const string SectionName = "Worker:IntegrationJobs";

    public int PollIntervalSeconds { get; init; } = 10;
    public int MaxJobsPerCycle { get; init; } = 5;
    public int MaxOutboxMessagesPerCycle { get; init; } = 10;
    public int MaxWebhookDeliveriesPerCycle { get; init; } = 10;
}
