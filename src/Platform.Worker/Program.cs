using Platform.Infrastructure.DependencyInjection;
using Platform.Worker.IntegrationJobs;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddOptions<IntegrationJobWorkerOptions>()
    .BindConfiguration(IntegrationJobWorkerOptions.SectionName)
    .Validate(options => options.PollIntervalSeconds > 0, "Worker:IntegrationJobs:PollIntervalSeconds must be greater than zero.")
    .Validate(options => options.MaxJobsPerCycle > 0, "Worker:IntegrationJobs:MaxJobsPerCycle must be greater than zero.")
    .Validate(options => options.MaxOutboxMessagesPerCycle > 0, "Worker:IntegrationJobs:MaxOutboxMessagesPerCycle must be greater than zero.")
    .Validate(options => options.MaxWebhookDeliveriesPerCycle > 0, "Worker:IntegrationJobs:MaxWebhookDeliveriesPerCycle must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddCatalogPersistence(builder.Configuration);
builder.Services.AddHostedService<IntegrationJobWorker>();

var host = builder.Build();
host.Run();
