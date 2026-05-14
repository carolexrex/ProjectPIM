# Platform.Worker

`Platform.Worker` runs background polling loops for:

- integration jobs
- outbox fanout
- webhook delivery execution

## Key Settings

Worker polling settings:

```json
{
  "Worker": {
    "IntegrationJobs": {
      "PollIntervalSeconds": 5,
      "MaxJobsPerCycle": 10,
      "MaxOutboxMessagesPerCycle": 20,
      "MaxWebhookDeliveriesPerCycle": 20
    }
  }
}
```

Webhook replay settings shared with the admin API:

```json
{
  "Webhooks": {
    "ManualReplayEnabled": true,
    "ManualReplayDelaySeconds": 60
  }
}
```

## Notes

- The worker does not perform webhook replay inline. Replay only reschedules a delivery; the worker executes it when `NextAttemptAtUtc` becomes runnable.
- Catalog persistence and webhook/outbox runtime options are registered through `Platform.Infrastructure`.
