# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

@AGENTS.md

## What this is

A standalone, API-first PIM / e-commerce backend built as a **modular monolith** on .NET 10 (`net10.0`). One deployable platform, one PostgreSQL database, internal modules with enforced boundaries (Catalog, Pricing, Inventory, Markets, Customers, Companies, Cart, Orders, Integrations, Storefront, Security/Auditing). Background design rationale lives in `docs/architecture-baseline.md` and `docs/domain-spec-v1.md`.

## Common commands

The solution file is `Platform.slnx` (XML solution format — pass it explicitly to `dotnet`).

```bash
# Build everything
dotnet build Platform.slnx

# Run all tests
dotnet test Platform.slnx

# Run a single test class or method (xunit)
dotnet test --filter "FullyQualifiedName~StorefrontCartApplicationServiceTests"
dotnet test --filter "FullyQualifiedName~StorefrontCartApplicationServiceTests.AddLine_Succeeds"

# Restore the local EF tool (pinned in dotnet-tools.json)
dotnet tool restore
```

### Full local dev stack

`scripts/start-dev.sh` (macOS/Linux) / `scripts/start-dev.ps1` (Windows) is the one-command path. It starts a Docker `projektpim-postgres` container (postgres:17), applies EF migrations, builds, then launches all four hosts as background processes with pids/logs under `.dev-runtime/`:

- Admin API → http://localhost:5053
- Storefront API → http://localhost:5064
- Backoffice UI → http://localhost:5168
- Worker → background processing (no HTTP)

Flags: `--skip-db` (Postgres already running), `--skip-migrate`. Stop with `scripts/stop-dev.sh` (`--keep-db` leaves the Postgres container running). Seed dev/admin users are defined in `src/Platform.Api/appsettings.Development.json` (e.g. `test` / `Test123!`).

To seed smoke catalog data and run the storefront cart/checkout gate (macOS/Linux): `scripts/seed-storefront-smoke.sh` (bash port of `seed-storefront-smoke.ps1`; requires `curl` + `jq`). It seeds the `WEB-SE`/`SE`/`example-drill` dataset, requests a projection rebuild, and exercises the 10-step cart/checkout gate (VAT, reservation, idempotency, ownership, oversell guard, cancellation release) from `docs/pim-roadmap-v1.md`.

### EF Core migrations

`dotnet-ef` is a local tool (run via `dotnet tool run dotnet-ef ...`). The single DbContext is `Platform.Infrastructure.Persistence.PlatformDbContext`, and both `--project` and `--startup-project` point at `src/Platform.Infrastructure`. The design-time factory reads the connection string from `Platform.Api`'s appsettings, so migrations always target Postgres regardless of the configured runtime provider.

```bash
dotnet tool run dotnet-ef migrations add <Name> \
  --project src/Platform.Infrastructure --startup-project src/Platform.Infrastructure \
  --context Platform.Infrastructure.Persistence.PlatformDbContext

dotnet tool run dotnet-ef database update \
  --project src/Platform.Infrastructure --startup-project src/Platform.Infrastructure \
  --context Platform.Infrastructure.Persistence.PlatformDbContext
```

EF migrations are the source of truth for the runtime schema. The `sql/` files are reference/seed scripts, not the active migration path.

## Architecture

### Layering (dependency direction enforced — see AGENTS.md)

`Domain` (no dependencies) ← `Application` ← `Infrastructure` ← host projects (`Api`, `StorefrontApi`, `Worker`, `Backoffice`). `Contracts` holds transport DTOs/validation shared across boundaries. Domain stays free of EF/HTTP/DTO concerns; provider-specific code lives only in Infrastructure.

### Application service + repository pattern

Each module exposes an `I…AdminApplicationService` (or storefront equivalent) interface in `Platform.Application/<Module>` defining commands, queries, and result DTOs. Implementations live in `Platform.Infrastructure/<Module>` and depend on persistence abstractions (`IUnitOfWork`, `I…Repository`) from `Platform.Application/Abstractions/Persistence`. Controllers stay thin: validate transport, call **one** application service, return DTOs or problem details.

### Dual persistence providers

`AddCatalogPersistence` in `Platform.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` is the composition root. It reads `Persistence:Provider` and wires either **PostgreSql** (EF/`EfUnitOfWork`) or **InMemory** (`InMemoryCatalogStore`/`InMemoryUnitOfWork`). Some services intentionally have InMemory-only implementations today (e.g. `InMemoryProductAdminApplicationService`, `InMemoryCategoryAdminApplicationService`). When adding a service, register both provider paths consistently. Tests use the EF InMemory provider via `Microsoft.EntityFrameworkCore.InMemory`.

### Two HTTP surfaces

- **`Platform.Api`** — admin/integration surface (full CRUD, bulk, webhooks). Auth is token-based admin identity with role/policy claims; users configured under `AdminSecurity`/`AdminIdentityToken`.
- **`Platform.StorefrontApi`** — read-optimized storefront (context resolution, product/category browse, cart). It reads **storefront product projections**, not live catalog tables.

### Storefront projection / outbox / webhook pipeline (the part that needs multiple files to understand)

Catalog writes don't directly update the storefront read model. Instead:

1. A catalog mutation enqueues a **storefront projection refresh request** and writes domain/integration events to an **outbox** (`IOutboxEventPublisher`).
2. **`Platform.Worker`** polls (intervals in `Worker:IntegrationJobs:*`) and processes, in order: integration jobs → storefront projection refresh requests → outbox fanout → webhook delivery. Refreshes run **before** webhook fanout so external consumers only see events after the local projection is current.
3. `IStorefrontProjectionBuilder` rebuilds projections; the refresh processor coalesces a batch to distinct product ids, falls back to per-message processing on failure, treats invalid payloads as poison (logged + marked published), and abandons batches after a retry limit so one bad refresh can't starve the worker.
4. Webhook **replay** never runs inline — it only reschedules a delivery's `NextAttemptAtUtc`; the worker executes it when runnable (`Webhooks:ManualReplay*`).

Because of this, freshly seeded Postgres data won't appear on storefront product endpoints until a projection rebuild or worker-processed refresh has run. See `docs/storefront-read-model-design-v1.md`, `docs/webhooks-and-outbox.md`, and `src/Platform.Worker/README.md`.

## Key docs

- `docs/architecture-baseline.md` — overall direction and module list
- `docs/domain-spec-v1.md`, `docs/sql-schema-v1.md` — domain model and schema
- `docs/admin-api-contract-v1.md`, `docs/storefront-api-contract-v1.md` — HTTP contracts
- `docs/local-database-bootstrap.md`, `docs/nexra-storefront-smoke.md` — local DB + storefront smoke seeding
- `docs/backoffice-ux-direction.md` — Backoffice UX/theme direction, future Natrium naming, and operations-console plan
