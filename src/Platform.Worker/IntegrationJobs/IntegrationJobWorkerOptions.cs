namespace Platform.Worker.IntegrationJobs;

public sealed class IntegrationJobWorkerOptions
{
    public const string SectionName = "Worker:IntegrationJobs";

    public int PollIntervalSeconds { get; init; } = 10;
    public int MaxJobsPerCycle { get; init; } = 5;
    public int MaxStorefrontProjectionRefreshMessagesPerCycle { get; init; } = 20;
    public int MaxOutboxMessagesPerCycle { get; init; } = 10;
    public int MaxWebhookDeliveriesPerCycle { get; init; } = 10;

    /// <summary>
    /// Enables the time-driven pass that refreshes storefront projections when price-list or
    /// price-list-entry validity boundaries pass without a write mutation.
    /// </summary>
    public bool StorefrontPriceValidityRefreshEnabled { get; init; } = true;

    /// <summary>
    /// How often the price-validity scan runs. It is gated inside the worker loop, so it is
    /// independent of <see cref="PollIntervalSeconds"/>.
    /// </summary>
    public int StorefrontPriceValidityScanIntervalSeconds { get; init; } = 60;

    /// <summary>
    /// How far back the scan still considers a passed boundary. This bounds the scan and lets it
    /// recover boundaries that passed while the worker was stopped.
    /// </summary>
    public int StorefrontPriceValidityLookbackMinutes { get; init; } = 1440;

    /// <summary>
    /// Upper bound on products enqueued for refresh by one price-validity scan. The cap applies to
    /// products the scan already determined are stale, so anything left over is picked up by a
    /// later scan rather than starved.
    /// </summary>
    public int MaxStorefrontPriceValidityProductsPerCycle { get; init; } = 100;
}
