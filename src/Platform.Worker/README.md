# Platform.Worker

`Platform.Worker` runs background polling loops for:

- integration jobs
- storefront projection refresh requests
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
      "MaxStorefrontProjectionRefreshMessagesPerCycle": 20,
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
- Storefront projection refresh requests are processed before webhook outbox fanout so webhook consumers see events after the local projection has been refreshed.
- Storefront projection refresh processing coalesces each polling batch to distinct product ids and logs processed message/product counts.
- Invalid storefront projection refresh payloads are treated as poison messages: they are logged and marked published so they do not block later refresh work.
- Catalog persistence and webhook/outbox runtime options are registered through `Platform.Infrastructure`.
