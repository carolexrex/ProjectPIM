# PIM Roadmap v1

This is the current implementation roadmap for the modular monolith in this repository.

It replaces ad hoc planning from chat history. Treat `sql-schema-v1.md` as a historical schema draft, not the active delivery plan.

## Current Direction

- Runtime database: `PostgreSQL`
- Architecture: API-first modular monolith
- Admin UI: optional `Platform.Backoffice` host on top of admin APIs
- Persistence: EF Core migrations as the source of truth for active schema

## Current Snapshot

Implemented in code today:

- admin authentication, bootstrap credentials, and catalog role policies
- audit log persistence and read APIs
- database-backed admin users, admin identity tokens, and identity/audit policies
- catalog domain/application/API/infrastructure flow for:
  - products and translations
  - variants
  - categories
  - product attributes and attribute options
  - product and variant media
  - product relations and bundle components
  - brands
  - markets and market product assignments
  - channels and channel-market assignments
  - price lists, entries, and market assignments
- inventory locations, balances, transactions, and variant inventory snapshots
- customers, companies, addresses, and company memberships
- carts, orders, order status history, and payment transaction records
- persisted integration jobs, admin job endpoints, worker execution for brand/product import-export, and outbox/webhook delivery with first-pass catalog/pricing mutation event coverage
- PostgreSQL DbContext, configurations, migrations, and design-time factory
- initial backoffice screens for catalog, inventory, customer/company, and cart/order admin modules

Present but still skeletal:

- `Platform.StorefrontApi`, beyond the initial context, category, and first product read endpoints

## Implemented

- product and variant status definitions
- product CRUD, translations, and variant CRUD
- category management with hierarchy
- product and variant attribute definitions
- product category assignment
- variant attribute values
- product attribute values
- product relations and bundle components
- media asset library
- product and variant media assignment
- brand management
- market management
- channel management
- pricing and price lists
- inventory locations, balances, and transactions
- customer and company administration
- cart and order administration foundations
- integration job persistence, brand/product import-export background execution, and webhook/outbox foundation with catalog/pricing mutation events
- bootstrap authentication and role policies
- audit log read APIs and admin user management foundations
- PostgreSQL migrations and local bootstrap path
- initial backoffice screens for catalog and commerce administration

## Active Documentation Map

- `AGENTS.md`: repo and layer boundaries
- `architecture-baseline.md`: broad architectural rationale and module direction
- `domain-spec-v1.md`: target business model and module responsibilities
- `admin-api-contract-v1.md`: target admin API resource surface
- `storefront-api-contract-v1.md`: target storefront/CMS-facing read contract
- `storefront-api-usage.md`: current developer usage guide for implemented storefront endpoints
- `storefront-read-model-design-v1.md`: implementation design for the next storefront projection/read-model slice
- `admin-api-implementation-design-v1.md`: target application/API implementation shape
- `ai-api-backoffice-contract.md`: planned AI workflow contract
- `local-database-bootstrap.md`: active PostgreSQL bootstrap path
- `sql-schema-v1.md`: historical SQL Server-first schema draft only

## Area Delivery Plan

The remaining work is best treated as explicit area plans rather than a short unordered backlog.

### 1. Inventory

Status:

- implemented

Scope:

- inventory locations
- market inventory-location assignments
- balances, transactions, and adjustment endpoints
- admin diagnostics for variant availability snapshots

Dependencies:

- variants
- markets

Exit criteria:

- domain and EF model exist for inventory entities
- admin API supports inventory location CRUD, balance upsert, and adjustment commands
- backoffice can view variant inventory and manage stock
- tests cover quantity validation and transaction/audit behavior

Completed:

- inventory locations, balances, and transactions are implemented
- admin API supports location CRUD, balance upsert, adjustment commands, and variant inventory snapshot reads
- backoffice supports inventory administration and variant inventory visibility
- migration `AddInventoryModule` exists

### 2. Customers And Companies

Status:

- implemented

Scope:

- customer CRUD and addresses
- company CRUD and addresses
- company memberships and B2B permission flags

Dependencies:

- stronger identity model planning
- admin authorization expansion beyond catalog roles

Exit criteria:

- customer/company aggregates and persistence mappings exist
- admin API supports customer, company, and membership workflows
- backoffice can administer customer/company records
- tests cover uniqueness, membership validity windows, and permission flags

Completed:

- customer/company aggregates, addresses, and company memberships are implemented
- admin API supports customer/company CRUD, address add, and membership create/update flows
- backoffice supports customer/company administration
- tests cover customer email uniqueness, membership validity windows, and permission flags

### 2A. Agentic Shopping Foundation

Status:

- planned explicitly
- partially enabled now through cart/order idempotency and company permission evaluation

Purpose:

- make the platform usable by automated buyers, copilots, and shopping agents without turning the PIM into a CMS

Scope:

- stable machine-readable product and variant identifiers across admin and storefront surfaces
- structured attribute/value resolution suitable for agent consumption
- explicit availability, price, and buyability diagnostics on storefront-oriented reads
- customer/company context that can constrain what an agent may buy or approve
- cart/order actions that are deterministic, idempotent, and explainable
- audit and guardrail support for automated decision-making and assisted purchasing

Non-goals:

- embedding a shopping chatbot in backoffice
- moving editorial campaign content ownership into the PIM
- making CMS content the source of truth for buyability or commercial rules

Dependencies:

- customers/companies
- carts/orders
- storefront read model and search
- audit/history and stronger identity

Exit criteria:

- storefront and commerce APIs expose explicit structured data instead of UI-shaped text blobs
- cart/order workflows support idempotency keys and clear failure reasons
- company/member permissions can be evaluated cleanly for assisted purchasing flows
- roadmap items for storefront/search and AI workflows reference this foundation directly

### 3. Carts And Orders

Status:

- implemented

Scope:

- admin cart queries and actions
- order queries, status history, and payment transaction records
- cart-to-order orchestration foundation for later storefront flows

Dependencies:

- customers/companies
- inventory
- pricing

Exit criteria:

- domain/application support exists for cart and order records
- admin API exposes cart/order reads and order status transitions
- order creation path is idempotent
- tests cover status transitions and snapshot persistence rules

Completed:

- cart and order aggregates, addresses, status history, and payment transactions are implemented
- admin API supports cart reads, repricing, expiration, order reads, order creation, status changes, and payment transaction writes
- backoffice supports cart inspection, repricing, expiration, cart-to-order conversion, and order operations
- order creation from carts is idempotent and company permissions are enforced for company-context ordering
- migration `AddCartOrderModule` exists

### 4. Custom Fields And AI Content Workflow

Status:

- contract/spec exists
- not implemented

Scope:

- custom field definitions and value orchestration
- field capability metadata for AI-assisted workflows
- AI prompt templates, jobs, suggestions, and review actions

Dependencies:

- customers/catalog target writers
- worker/background execution
- audit/history

Exit criteria:

- custom fields are persisted and exposed through admin APIs
- AI suggestion workflow supports create, review, accept, reject, and edit
- accepted suggestions write through application abstractions rather than controller branching
- tests cover capability validation and idempotent suggestion acceptance

### 5. Import/Export And Background Jobs

Status:

- in progress

Scope:

- import/export job records
- bulk admin endpoints delegating long-running work
- background execution pipeline and retry-safe job processing
- outbound webhook/outbox foundation

Dependencies:

- worker composition root
- audit/history requirements

Exit criteria:

- bulk operations create persisted jobs instead of performing long work inline
- worker can execute at least one real job type end-to-end
- outbox/integration job tables are introduced with clear ownership
- operational logging and failure reporting are visible to admins

Completed In Current Slice:

- `IntegrationJob` persistence model, repository abstractions, EF mappings, and migration are implemented
- admin API supports creating brand/product import-export jobs, listing integration job status/details, and administering webhook subscriptions/delivery visibility
- `Platform.Worker` now hosts a polling background worker instead of only the default template
- the worker can execute brand/product import-export jobs end-to-end with persisted result payloads, summaries, retries, and admin-visible failure state
- outbox messages, webhook subscriptions, and webhook deliveries are persisted, with worker fanout and retry-safe delivery processing
- catalog and pricing admin mutations now publish outbox events for brand/product create-update flows and price-list create-update flows
- webhook delivery replay is now configuration-gated, admin-invokable, and scheduled through a configurable replay delay instead of running inline
- tests cover brand/product import-export workflows, catalog/pricing outbox event publication, and end-to-end outbox-to-webhook delivery processing

### 6. Storefront Read Model And Search

Status:

- in progress
- `Platform.StorefrontApi` now includes context, category, and first product read endpoints, but the broader read model/search surface is still missing

Scope:

- read-optimized product/category endpoints
- market, culture, price, and availability resolution for storefront consumers
- initial search/browse strategy

Dependencies:

- inventory
- cart/order foundations
- market, pricing, and catalog completeness
- agentic shopping foundation

Exit criteria:

- storefront API exposes context, category, and product read endpoints for CMS/storefront consumers
- read path does not reuse admin-oriented response shapes directly
- search approach is chosen explicitly: PostgreSQL-first or dedicated search projection
- tests cover culture fallback and buyability resolution inputs
- agent-facing reads expose explicit buyability, pricing, and availability diagnostics

Completed In Current Slice:

- `GET /api/storefront/context` resolves channel, market, culture, and currency context for storefront/CMS consumers
- `GET /api/storefront/categories` returns the localized storefront category tree
- `GET /api/storefront/categories/{slug}` returns localized category detail with breadcrumb and child-category data
- `GET /api/storefront/products` returns paged localized product summaries with market/category/brand filtering and structured price, availability, and buyability diagnostics
- `GET /api/storefront/products/{slug}` returns localized product detail with categories, media, attributes, variants, and structured commerce diagnostics
- `GET /api/storefront/products/by-number/{productNumber}` resolves product detail through a stable commerce identifier for CMS/integration consumers
- `StorefrontProductProjection` foundation is implemented with projection entity, repository, builder, refresh service, EF configuration, and migration `AddStorefrontProductProjection`
- admin API and worker now support a real storefront projection rebuild job, and storefront product browse/detail read through the projection repository
- direct product, variant, brand, category subtree, price-list entry, and inventory-balance mutations now enqueue targeted storefront projection refresh requests processed by the worker
- storefront projection refresh requests are internal outbox messages processed separately from external webhook fanout, and storefront reads no longer perform full rebuilds on read misses
- storefront context resolution supports explicit channel/market input and host-name-based channel lookup
- product browse now exposes supported sort values plus category/brand facet metadata for storefront consumers
- tests cover market/channel resolution, host-name resolution, ambiguity handling, storefront category localization, category breadcrumbs, product visibility, product-number lookup, product browse facets/sorting, projection building/refresh, price/inventory resolution, and culture/currency fallback behavior

### 7. Audit, History, And Stronger Identity

Status:

- in progress

Scope:

- audit log model for important write actions
- richer identity model beyond bootstrap users
- expanded role/policy coverage for non-catalog modules
- support for integration clients and future scoped permissions

Dependencies:

- customers/companies
- orders
- AI review flows

Exit criteria:

- important write workflows emit audit records
- admin identity is no longer limited to bootstrap-only users
- policies exist for catalog, pricing, inventory, customer service, and AI review areas
- documentation clearly distinguishes runtime auth, admin auth, and future integration auth

Completed In Current Slice:

- audit log entity, persistence model, and admin read API are implemented
- admin writes can be attributed to authenticated actor identity through the API token claims
- database-backed admin users are implemented with bootstrap user fallback still preserved for recovery/dev access
- admin tokens now distinguish principal type so admin and integration callers are no longer treated as the same kind of actor
- admin user list/get/create/update API is implemented
- tests cover admin token principal-type claims and admin user password hashing / role persistence

## Recommended Sequencing

1. audit/history and stronger identity
2. import/export and background jobs
3. custom fields and AI workflow
4. storefront read model and search
5. agentic shopping refinement on top of storefront and commerce flows

## Integration Job Execution Refactor Plan

The current integration job execution service should be split before adding more job types.

Recommended shape:

1. Keep `IntegrationJobExecutionService` as the orchestrator that claims runnable jobs, starts them, dispatches to a handler, records completion/failure, and publishes job lifecycle events.
2. Introduce one handler per job type or job family: brand export, brand import, product export, product import, and storefront projection rebuild.
3. Move import validation and row mapping into handler-local collaborators where it materially reduces constructor size or makes tests narrower.
4. Add a registry keyed by `IntegrationJobTypes` so unsupported job types fail in one place.
5. Preserve the existing job state transitions and outbox publication tests during the split, then add focused handler tests for import/export edge cases.

## Modeling Notes

- Bundle support is currently modeled through `ProductRelation` with relation type `BundleComponent` plus `Quantity`.
- Product relations currently support:
  - `RelatedProduct`
  - `Accessory`
  - `BundleComponent`
- Backoffice remains API-first. UI code must not bypass the admin API.

## Documentation Status

- The roadmap in this file is the active execution plan for unfinished modules.
- Implemented areas should be updated here when code lands so the roadmap remains an execution artifact rather than stale notes.
- `domain-spec-v1.md` and `admin-api-contract-v1.md` describe the target end-state more broadly than the code currently implements.
- `sql-schema-v1.md` is not the active runtime plan and should only be used as historical design context.
