# Agent Handover - ProjectPIM

Date: 2026-05-21

Use this as the opening prompt/context for a fresh coding agent.

## Handover Prompt

You are working in `C:\Projects\ProjectPIM`, a modular monolith PIM built with .NET. Read `AGENTS.md` first and preserve the project boundaries:

- `Platform.Domain`: domain entities, invariants, domain exceptions only.
- `Platform.Application`: use-case contracts, commands, queries, persistence abstractions.
- `Platform.Infrastructure`: EF/persistence, integrations, runtime implementations.
- `Platform.Api`: admin/integration HTTP surface.
- `Platform.StorefrontApi`: read-optimized storefront HTTP surface only.
- `Platform.Backoffice`: UI host consuming APIs.
- `Platform.Worker`: background jobs and async processing.
- `Platform.Contracts`: shared transport DTOs/validation attributes.

The previous work completed the storefront read-model Phase 3 reliability slice. Before starting new work, inspect:

- `docs/pim-roadmap-v1.md`
- `docs/storefront-api-contract-v1.md`
- `docs/storefront-api-usage.md`
- `docs/storefront-read-model-design-v1.md`
- `docs/webhooks-and-outbox.md`
- `src/Platform.Worker/README.md`
- `src/Platform.Infrastructure/Storefront/StorefrontProjectionOutboxProcessor.cs`
- `src/Platform.Domain/Integrations/OutboxMessage.cs`
- `tests/Platform.Tests/StorefrontProjectionOutboxProcessorTests.cs`

## Current State

The storefront API/read-model slice has passed the first read-only Nexra CMS proof-of-integration against the local PostgreSQL smoke seed.

Implemented storefront-facing capabilities:

- `GET /api/storefront/context`
- `GET /api/storefront/categories`
- `GET /api/storefront/categories/{slug}`
- `GET /api/storefront/products`
- `GET /api/storefront/products/{slug}`
- `GET /api/storefront/products/by-number/{productNumber}`
- `POST /api/storefront/carts`
- `GET /api/storefront/carts/{id}`
- `POST /api/storefront/carts/{id}/reprice`
- `POST /api/storefront/carts/{id}/checkout`
- market/culture/currency-aware storefront reads
- row-version guarded storefront cart repricing and checkout
- signed cart access-token ownership proof for cart read/reprice/checkout
- idempotent cart checkout by source cart id
- projection-backed storefront cart buyability validation
- checkout email, billing-address, and shipping-address validation
- projection-backed product browse/detail reads
- category/brand facets and supported sort metadata
- structured price, availability, and buyability diagnostics

Storefront security direction:

- admin/backoffice APIs should always be authenticated and authorization-checked
- storefront catalog reads should be configurable per deployment/channel: `Public`, `TrustedClientsOnly`, or `Private`
- public catalog reads still need CORS, rate limits, and gateway controls
- browser-visible API keys are identifiers, not secrets
- cart and checkout endpoints now require signed cart access-token ownership proof; future shopper/customer/session auth can layer on top

Local development note:

- `Platform.Api` runs on `http://localhost:5053`
- `Platform.StorefrontApi` runs on `http://localhost:5064`
- `Platform.Backoffice` runs on `http://localhost:5168`
- `Platform.Worker` should run for projection rebuild jobs, incremental storefront refreshes, outbox fanout, and webhook delivery
- storefront endpoint examples should use `http://localhost:5064/api/storefront/...` unless a gateway/proxy is configured
- `http://localhost:5064` is the Storefront API host/origin; `http://localhost:5064/api/storefront` is the canonical local Storefront API base URL
- the `WEB-SE`/`SE` example context exists in the in-memory demo store, but a freshly migrated PostgreSQL database does not seed that demo catalog
- live PostgreSQL storefront smoke tests need matching channel/market/catalog/pricing/inventory data plus built storefront product projections; use `scripts/seed-storefront-smoke.ps1`
- the tested Nexra smoke context is channel `WEB-SE`, market `SE`, culture `sv-SE`, currency `SEK`, product slug `example-drill`, and product number `SKU-EXAMPLE-1`

Implemented Phase 3 incremental refresh coverage:

- product mutations enqueue targeted storefront refresh
- variant mutations enqueue targeted storefront refresh
- brand mutations fan out to affected products
- category subtree mutations fan out to affected products
- market product assignment changes fan out to affected products
- inventory-location market assignment changes fan out to affected products
- price-list entry changes fan out to affected products/variants
- inventory-balance changes fan out to affected variants/products

Implemented reliability hardening:

- internal storefront refresh requests are outbox messages with event type `storefront.product-projection.refresh-requested`
- webhook fanout ignores internal storefront refresh messages
- storefront refresh processor polls only runnable internal messages
- processor coalesces each polling batch to distinct product ids
- invalid refresh payloads are treated as poison messages and marked published after warning logs
- failed refresh messages persist:
  - `ProcessingAttemptCount`
  - `LastProcessingError`
  - `NextProcessingAttemptAtUtc`
  - `ProcessingAbandonedAtUtc`
- retry uses exponential backoff capped at 30 minutes
- repeated failures are abandoned after 5 attempts
- failed coalesced batches fall back to per-message processing so one bad refresh does not retry/abandon healthy messages
- EF pending storefront projection changes are discarded before retry state is saved after a failed refresh

Important migration files for the retry state:

- `src/Platform.Infrastructure/Persistence/Migrations/20260518212055_AddOutboxProcessingState.cs`
- `src/Platform.Infrastructure/Persistence/Migrations/20260518212055_AddOutboxProcessingState.Designer.cs`
- `src/Platform.Infrastructure/Persistence/Migrations/PlatformDbContextModelSnapshot.cs`

Last known verification after the reliability slice:

```powershell
dotnet test Platform.slnx --no-restore
```

Expected result at handover time: 76 tests passing.

## Completed Nexra Proof

The read-only Nexra CMS proof-of-integration has completed successfully against the Storefront API.

Do not start with PIM admin/write integration from the CMS. The normal integration shape is:

- CMS consumes storefront/catalog reads.
- Storefront/purchase layer consumes cart/checkout/order APIs when needed.
- CMS admin integration is optional later, usually for product pickers, previews, or editorial merchandising references, not product editing.

Validated Nexra flow:

1. Compared Nexra page needs to `docs/storefront-api-contract-v1.md` and `docs/storefront-api-usage.md`.
2. Exercised page scenarios:
   - category navigation
   - category landing page
   - product listing page
   - product detail page by slug
   - product lookup by stable product number
   - unavailable/not-buyable products
   - missing slug/product responses
   - market/culture/currency switching
3. No ProjectPIM storefront contract gap was identified by this smoke.

Nexra connector note:

- ProjectPIM documents `http://localhost:5064/api/storefront` as the base URL.
- `http://localhost:5064` is only the host/origin.
- Nexra now accepts both configuration forms, but callers must avoid double-prefixing `/api/storefront`.

## Operator Visibility Added After Nexra Smoke

Storefront refresh/outbox operator visibility has been added:

- admin API lists pending/delayed/abandoned/published internal storefront refresh messages
- admin API exposes retry count, last error, next retry time, abandoned timestamp, and row version
- backoffice has a `Storefront Ops` view for open refresh messages
- abandoned refresh messages can be reset from admin API/backoffice

Implemented domain shape for reset:

- keep `Published` as successful or deliberately consumed processing
- keep `Abandoned` as terminal automatic failure after retries
- `OutboxMessage.ResetProcessingForReplay` clears abandoned/retry state and makes the message runnable again

Do not overload `MarkPublished` for replay.

Current admin endpoints:

- `GET /api/admin/storefront-projection-refresh-messages?status=open`
- `GET /api/admin/storefront-projection-refresh-messages/{id}`
- `POST /api/admin/storefront-projection-refresh-messages/{id}/reset`

Backoffice route:

- `http://localhost:5168/storefront-operations`

## Suggested Next Step

The first storefront cart/checkout path is now implemented and protected with signed cart access-token ownership proof. Suggested next work is to validate it with a live smoke from the consuming storefront/Nexra side.

Smoke checklist:

1. resolve storefront context against `http://localhost:5064/api/storefront`
2. read product detail and select a visible, buyable variant
3. create a cart and capture `rowVersion` plus `cartAccessToken`
4. read and reprice the cart using `X-Storefront-Cart-Token`
5. checkout using `X-Storefront-Cart-Token` and the latest `rowVersion`
6. repeat checkout to confirm idempotency by source cart id
7. confirm missing or invalid `X-Storefront-Cart-Token` returns `401`

After that smoke passes, design the next commerce slice: payment initiation/callbacks or fuller cart mutation operations such as adding/removing/updating lines after cart creation.

AI is intentionally later in the plan. The final AI stages should be:

1. AI product content proposals: product copy, SEO fields, translations, and media alt text as reviewable proposals.
2. AI attribute/category/import enrichment: suggested attributes, category/facet placement, supplier import mapping, and anomaly hints as reviewable proposals.

Do not start with unrestricted chat writes. AI output should first be persisted as proposals and applied only through existing application services after operator review.

## Commit Guidance

For the completed retry/backoff slice, a good commit message was:

```text
Harden storefront projection refresh retries
```

Suggested body:

```text
Add persisted retry/backoff state for internal storefront projection refresh messages.

Coalesce refresh batches, fall back to per-message processing when a batch fails, discard pending EF projection changes before saving retry state, and abandon repeatedly failing messages after the retry limit.

Update storefront/outbox documentation and add processor coverage for retry, abandonment, poison payloads, and fallback behavior.
```
