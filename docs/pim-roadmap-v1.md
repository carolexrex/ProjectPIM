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

### 4. Custom Fields And AI Workflow Foundation

Status:

- contract/spec exists
- not implemented

Scope:

- custom field definitions and value orchestration
- field capability metadata for AI-assisted workflows
- AI provider/model configuration and feature profiles
- AI prompt templates, jobs, proposals, suggestions, and review actions
- audit fields for provider, model, prompt version, input snapshot, output, actor, and approval state

Dependencies:

- customers/catalog target writers
- worker/background execution
- audit/history
- stronger backoffice review UX

Exit criteria:

- custom fields are persisted and exposed through admin APIs
- AI proposal workflow supports create, review, accept, reject, and edit
- accepted proposals write through application abstractions rather than controller branching
- model choice is controlled by admin-configured provider profiles rather than arbitrary per-user model selection
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
- direct product, variant, brand, category subtree, market product assignment, inventory-location market assignment, price-list entry, and inventory-balance mutations now enqueue targeted storefront projection refresh requests processed by the worker
- storefront projection refresh requests are internal outbox messages processed separately from external webhook fanout, and storefront reads no longer perform full rebuilds on read misses
- storefront projection refresh processing now persists retry/backoff state so repeated failures are observable, delayed, and eventually abandoned instead of retried every worker tick
- storefront context resolution supports explicit channel/market input and host-name-based channel lookup
- product browse now exposes supported sort values plus category/brand facet metadata for storefront consumers
- storefront cart endpoints now support cart creation, cart reads, projection-backed buyability validation, signed cart access-token ownership proof, row-version guarded repricing, and cart checkout into an order
- tests cover market/channel resolution, host-name resolution, ambiguity handling, storefront category localization, category breadcrumbs, product visibility, product-number lookup, product browse facets/sorting, projection building/refresh, price/inventory resolution, and culture/currency fallback behavior
- tests cover storefront cart creation, pricing, buyability validation, cart access-token ownership proof, checkout address validation, checkout conversion, idempotent checkout, and empty-cart validation

### 7. Audit, History, And Stronger Identity

Status:

- in progress

Scope:

- audit log model for important write actions
- richer identity model beyond bootstrap users
- expanded role/policy coverage for non-catalog modules
- support for integration clients and future scoped permissions
- storefront security configuration for public, trusted-client, and private catalog read modes
- shopper/session/cart ownership proof for storefront commerce endpoints

Dependencies:

- customers/companies
- orders
- AI review flows

Exit criteria:

- important write workflows emit audit records
- admin identity is no longer limited to bootstrap-only users
- policies exist for catalog, pricing, inventory, customer service, and AI review areas
- documentation clearly distinguishes runtime auth, admin auth, and future integration auth
- storefront catalog read protection is configurable per deployment/channel
- storefront cart/checkout access is protected by shopper, session, or cart ownership proof

Completed In Current Slice:

- audit log entity, persistence model, and admin read API are implemented
- admin writes can be attributed to authenticated actor identity through the API token claims
- database-backed admin users are implemented with bootstrap user fallback still preserved for recovery/dev access
- admin tokens now distinguish principal type so admin and integration callers are no longer treated as the same kind of actor
- admin user list/get/create/update API is implemented
- storefront cart read/reprice/checkout require signed cart access-token ownership proof
- tests cover admin token principal-type claims and admin user password hashing / role persistence

### 8. AI Product Content Proposals

Status:

- planned as a final-stage AI feature
- depends on the AI workflow foundation

Purpose:

- help operators generate and review product-facing content without allowing unreviewed model writes into catalog data

Scope:

- product name, short description, long description, SEO title, and SEO description proposals
- localized translation proposals for existing product content
- media alt-text proposals from product images
- prompt/profile versioning so outputs can be audited and regenerated intentionally

Non-goals:

- direct chat-driven product writes
- synthetic primary product imagery as a default workflow
- replacing merchant approval for customer-facing copy

Dependencies:

- AI workflow foundation
- product and media admin APIs
- audit/history and role policy coverage
- backoffice proposal review UI

Exit criteria:

- operators can request product content proposals for one or more products
- proposals are persisted with model/provider/prompt metadata and input snapshots
- accepted proposals update products through existing application services with row-version checks
- rejected or edited proposals remain auditable

### 9. AI Attribute, Category, And Import Enrichment

Status:

- planned as the final AI feature stage after content proposals

Purpose:

- help operators improve product data completeness and supplier onboarding quality while keeping deterministic approval and validation in ProjectPIM

Scope:

- suggest missing product/variant attributes from descriptions, existing attributes, and media context
- suggest category/facet placement for products
- assist supplier import mapping for CSV/Excel-like product feeds
- detect likely data quality issues and inventory/catalog anomalies for operator review

Non-goals:

- automatic inventory mutation without explicit approval
- bypassing category, attribute, pricing, inventory, or product validation rules
- free-form agent writes outside the proposal/review/apply pipeline

Dependencies:

- AI product content proposal workflow
- import/export job pipeline
- product attribute and category administration
- stronger backoffice bulk review UX

Exit criteria:

- enrichment suggestions are persisted as reviewable proposals
- accepted suggestions apply through existing application services and validation rules
- supplier import mapping assistance can produce a reviewable mapping before a job is submitted
- tests cover proposal acceptance, validation failures, and idempotent re-application behavior

## Recommended Sequencing

1. validate the storefront cart/checkout path with a live smoke from the consuming storefront/Nexra side
2. choose the next commerce slice: payment initiation/callbacks or fuller cart line mutation endpoints
3. improve backoffice review/bulk-edit UX where AI proposals will later be reviewed
4. complete audit/history and stronger identity gaps that affect proposal approval and integration clients
5. finish import/export and background job refactoring
6. complete custom fields and AI workflow foundation
7. add AI product content proposals
8. add AI attribute, category, and import enrichment
9. refine agentic shopping flows on top of storefront and commerce APIs

## Immediate Next Step

Run a live storefront cart/checkout smoke against `Platform.StorefrontApi` from the consuming storefront or Nexra integration side.

The smoke should use the canonical local base URL `http://localhost:5064/api/storefront` and the current cart ownership flow:

1. resolve context for the smoke channel/market/culture/currency
2. read product detail and select a visible, buyable variant
3. create a cart and capture both `rowVersion` and `cartAccessToken`
4. read the cart with `X-Storefront-Cart-Token`
5. reprice the cart with `X-Storefront-Cart-Token` and the latest `rowVersion`
6. checkout the cart with `X-Storefront-Cart-Token`, billing address, shipping address, and the latest `rowVersion`
7. repeat checkout once to confirm idempotency by source cart id
8. verify missing or invalid `X-Storefront-Cart-Token` returns `401`

Passing this smoke is the gate before starting the next commerce slice. After the smoke, choose between payment initiation/callbacks and fuller cart line mutation endpoints.

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
