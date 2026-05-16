# Webhooks And Outbox

This document describes the current webhook/outbox behavior in the platform.

## Current Event Coverage

The worker currently delivers webhook events for:

- `integration.job.completed`
- `integration.job.failed`
- `catalog.brand.created`
- `catalog.brand.updated`
- `catalog.product.created`
- `catalog.product.updated`
- `pricing.price-list.created`
- `pricing.price-list.updated`

The `updated` events intentionally cover sub-mutations on the aggregate, not only the top-level update method. For example, translation, relation, media, market-assignment, and price-entry changes are emitted as `updated` events with a more specific `changeType` in the payload.

## Internal Outbox Messages

The outbox table is also used for internal worker scheduling messages, currently:

- `storefront.product-projection.refresh-requested`

Internal messages are not webhook events. The storefront projection processor claims only refresh-request messages and marks them published after the local projection refresh has completed. Webhook fanout claims only externally supported webhook event types, so internal refresh requests cannot be marked published by webhook delivery processing.

## Delivery Lifecycle

Webhook delivery records move through these states:

- `Pending`: newly created and runnable immediately
- `Processing`: currently being sent by the worker
- `Succeeded`: terminal success state
- `Failed`: retryable failure, controlled by `NextAttemptAtUtc`
- `Abandoned`: terminal failure state for non-retriable cases

Current worker behavior:

- inactive or missing subscriptions abandon the delivery
- non-retriable client failures abandon the delivery
- transient failures become `Failed` and are retried later
- cancellation during shutdown is not swallowed into a synthetic retry result

## Manual Replay

Manual replay is an admin operation on existing webhook deliveries.

Current API:

- `POST /api/admin/webhook-deliveries/{id}/replay`

Request body:

```json
{
  "rowVersion": "..."
}
```

Rules:

- replay must be explicitly enabled in configuration
- only `Failed` and `Abandoned` deliveries can be replayed
- replay does not execute inline; it reschedules the delivery for worker pickup
- replay uses the configured delay rather than an ad hoc per-request value

Configuration:

```json
{
  "Webhooks": {
    "ManualReplayEnabled": true,
    "ManualReplayDelaySeconds": 60
  }
}
```

Meaning:

- `ManualReplayEnabled`: turns the admin replay action on or off
- `ManualReplayDelaySeconds`: cooldown before the replayed delivery becomes runnable

## Payload Shape

Webhook payloads are event-specific DTO snapshots. Current catalog/pricing payload contracts live in:

- `src/Platform.Contracts/Integrations/WebhookEventPayloads.cs`

Each payload includes:

- `occurredAtUtc`
- `changeType`
- the aggregate snapshot DTO

## Operational Notes

- The outbox message and aggregate mutation are written in the same unit of work.
- Worker delivery is intentionally decoupled from the write path.
- Internal outbox processors and webhook fanout use event-type-specific reads; avoid broad unpublished-message polling in new worker paths.
- Admin delivery detail views are the primary operational surface today.
- Replay is meant for controlled recovery, not bulk redrive automation.
