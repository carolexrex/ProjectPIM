# Platform.Worker

`Platform.Worker` runs background polling loops for:

- integration jobs
- storefront price-validity boundary scanning
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
      "MaxWebhookDeliveriesPerCycle": 20,
      "StorefrontPriceValidityRefreshEnabled": true,
      "StorefrontPriceValidityScanIntervalSeconds": 30,
      "StorefrontPriceValidityLookbackMinutes": 1440,
      "MaxStorefrontPriceValidityProductsPerCycle": 100
    }
  }
}
```

Price-validity settings:

- `StorefrontPriceValidityRefreshEnabled` turns the time-driven pass on or off.
- `StorefrontPriceValidityScanIntervalSeconds` gates the scan inside the worker loop, so the scan cadence is independent of `PollIntervalSeconds`.
- `StorefrontPriceValidityLookbackMinutes` is how far back a passed boundary is still picked up. It bounds the scan and is also the recovery window after worker downtime: boundaries that passed longer ago than this need an admin-triggered projection rebuild.
- `MaxStorefrontPriceValidityProductsPerCycle` bounds how many products one scan enqueues. The cap is applied to products the scan query already determined are stale *and* not already scheduled, so remaining stale products are guaranteed to be picked up by later scans rather than starved behind already-current or already-queued products. Keep `MaxStorefrontProjectionRefreshMessagesPerCycle` at or above this value if you want a scan's requests to drain in the same cycle; a lower value only delays them, it cannot duplicate them.

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
- The price-validity scan runs before refresh-message processing in the same worker cycle, so the refresh requests it enqueues are consumed promptly. It selects a product only when the product's oldest `ProjectedAtUtc` is at or before the passed boundary, so a rebuilt product stops being selected without any persisted watermark.
- Duplicate suppression does not depend on that ordering. Each scheduled request carries a product-scoped `OutboxMessage.DedupKey`, and a PostgreSQL partial unique index allows at most one *unpublished* message per key. Repeated scans, and multiple Worker instances scanning at the same time, therefore converge on one request per product. Because a refresh rebuilds the product at the instant it runs, that one request also covers boundaries that pass while it is waiting out a retry delay — the delay is never bypassed by a fresh message. The in-memory provider enforces the same rule. A key becomes reusable only once its message is published.
- Abandoned requests keep their dedup reservation, and the scan skips products that hold one. A price-validity refresh that exhausted its retries is therefore never silently replaced by a fresh message: it stays visible as an abandoned message and is returned to service with the admin reset workflow, which reuses the same message. The trade-off is that the product's projection stays stale until an operator acts, so keep an eye on the abandoned storefront-refresh message count.
- The scan enqueues one message per product, so the per-message retry path below isolates a failing product: the other products from the same scan are refreshed and published while only the failing one is delayed, retried, and eventually abandoned.
- Running several Worker instances is safe. If two instances select the same product in the same instant, the losing insert is rejected by the uniqueness constraint and that pass ends early; its remaining candidates are still stale and are picked up on the next scan.
- Storefront projection refresh processing coalesces each polling batch to distinct product ids, falls back to per-message processing when a coalesced batch fails, and logs processed message/product counts.
- Invalid storefront projection refresh payloads are treated as poison messages: they are logged and marked published so they do not block later refresh work.
- Failed storefront projection refresh batches record attempt count, last error, and next retry time. Repeated failures are abandoned after the processor limit so one bad refresh cannot consume worker capacity indefinitely.
- Catalog persistence and webhook/outbox runtime options are registered through `Platform.Infrastructure`.
