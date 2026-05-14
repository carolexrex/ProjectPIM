using Microsoft.Extensions.Options;
using Platform.Application.Integrations;

namespace Platform.Worker.IntegrationJobs;

public sealed class IntegrationJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<IntegrationJobWorkerOptions> _options;
    private readonly ILogger<IntegrationJobWorker> _logger;

    public IntegrationJobWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<IntegrationJobWorkerOptions> options,
        ILogger<IntegrationJobWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, _options.Value.PollIntervalSeconds));
        var maxJobsPerCycle = Math.Max(1, _options.Value.MaxJobsPerCycle);
        var maxOutboxMessagesPerCycle = Math.Max(1, _options.Value.MaxOutboxMessagesPerCycle);
        var maxWebhookDeliveriesPerCycle = Math.Max(1, _options.Value.MaxWebhookDeliveriesPerCycle);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var integrationJobExecutionService = scope.ServiceProvider.GetRequiredService<IIntegrationJobExecutionService>();
                var outboxExecutionService = scope.ServiceProvider.GetRequiredService<IWebhookOutboxExecutionService>();
                var webhookDeliveryExecutionService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryExecutionService>();

                var executed = await integrationJobExecutionService.ExecutePendingAsync(maxJobsPerCycle, stoppingToken);
                var published = await outboxExecutionService.ExecutePendingAsync(maxOutboxMessagesPerCycle, stoppingToken);
                var delivered = await webhookDeliveryExecutionService.ExecutePendingAsync(maxWebhookDeliveriesPerCycle, stoppingToken);

                if (executed > 0)
                {
                    _logger.LogInformation("Executed {JobCount} integration job(s).", executed);
                }

                if (published > 0)
                {
                    _logger.LogInformation("Published {MessageCount} outbox message(s).", published);
                }

                if (delivered > 0)
                {
                    _logger.LogInformation("Processed {DeliveryCount} webhook delivery(s).", delivered);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Integration job worker loop failed.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }
}
