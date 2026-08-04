# Import Source Onboarding Design v1

## Purpose

ProjectPIM should support assisted migration from an existing storefront or legacy PIM without pretending that every external system has the same structure.

The recommended product shape is one shared import platform with multiple source adapters:

- file feeds such as CSV or Excel
- authorized website discovery
- legacy REST APIs
- purpose-built connectors for systems that justify deeper support

AI may inspect documentation, schemas, samples, and ambiguous product pages to propose a mapping. The approved mapping must become a versioned, deterministic connector definition before a production import runs.

## Guiding Principles

1. Import into a staging area before mutating catalog, media, pricing, or inventory.
2. Prefer structured source data over visual or free-form extraction.
3. Use AI for discovery and proposals, not unrestricted production writes.
4. Make every import dry-runnable, reviewable, idempotent, and auditable.
5. Preserve source identity so reruns update existing records instead of creating duplicates.
6. Apply accepted data through existing application services and validation rules.
7. Start with one-time migrations; add scheduled synchronization only after update and deletion semantics are explicit.

## Shared Import Flow

All source types should use the same high-level workflow:

1. Register and authorize the source.
2. Discover its capabilities and retrieve a bounded sample.
3. Propose field, relationship, and value mappings.
4. Review and approve a versioned connector definition.
5. Run a dry run into staging.
6. Validate, deduplicate, and show a summary with blocking issues and warnings.
7. Approve and enqueue a deterministic import job.
8. Apply through normal product, variant, category, media, and pricing application services.
9. Persist per-item outcomes, source lineage, and an audit trail.

An import summary should distinguish discovered, staged, created, updated, unchanged, skipped, and failed records. Uncertain mappings must remain visible instead of being silently guessed.

## Source Identity And Idempotency

Imported records need stable lineage. At minimum, retain:

- source id and source type
- source object type and external object id
- source URL when applicable
- source fingerprint or version
- connector-definition version
- first imported and last observed timestamps
- resulting ProjectPIM entity id

The combination of source, object type, and external object id should be unique. If a source does not expose stable identifiers, derive a candidate key and require review because URL, SKU, and content hashes can all change.

## Authorized Website Discovery

Website discovery is intended for customer-owned storefronts or other sources the customer is authorized to migrate.

Discovery order:

1. product feeds and sitemaps
2. JSON-LD and other structured product metadata
3. embedded application state or documented storefront APIs
4. static HTML extraction
5. rendered-page extraction as a controlled fallback
6. AI-assisted interpretation only for fields or relationships that remain ambiguous

Candidate extraction may include:

- product names, descriptions, brands, SKUs, and GTINs
- variants, option names, and option values
- category membership
- product and variant images
- currency-aware current and previous prices
- availability signals when clearly represented
- canonical source URLs

The first release should be a bounded, one-time migration assistant. Continuous crawling is deferred because it requires explicit scheduling, change detection, source deletion semantics, price-history behavior, and handling for storefront layout changes.

Website discovery must enforce crawl limits, allowed hosts, timeouts, response-size limits, content-type checks, and media download limits. It should respect source access restrictions and record who confirmed authorization to scan the source.

## Legacy REST API Onboarding

Legacy PIM onboarding should accept:

- base URL
- authentication configuration stored through a secret reference
- OpenAPI document when available
- API documentation and representative response samples when OpenAPI is unavailable
- optional operator hints about product, variant, category, media, and price endpoints

The discovery process may use AI to propose:

- endpoint and operation selection
- pagination, rate-limit, and retry behavior
- field and JSON-path mappings
- identifier and relationship mappings
- value transformations and enum mappings
- incremental cursor or modified-time behavior

Production execution must use the approved connector definition, not free-form agent decisions. Read-only discovery should be the default; mutating calls to a source system are outside the import use case.

Supporting every arbitrary API without review is not a v1 goal. OpenAPI-compatible and conventional paginated JSON APIs should be the first compatibility target. Sources with unusual authentication, workflows, or relationship semantics may require a purpose-built adapter.

## Staging And Validation

Staged candidates should be normalized into ProjectPIM-oriented shapes without becoming live entities. Validation should identify at least:

- missing or duplicate external identifiers
- duplicate SKU or GTIN conflicts
- products without variants when a sellable variant is required
- unresolved categories, brands, attribute definitions, or option values
- invalid currencies, price ranges, or overlapping price records
- missing market context for prices
- images that cannot be fetched, decoded, or safely accepted
- source records that would overwrite newer local changes

Pricing imports must not infer tax treatment, market assignment, or currency when the source is ambiguous. Inventory should remain a separate opt-in mapping and must never be mutated merely because a webpage displays an availability label.

## Security And Operations

External source access requires:

- encrypted secret storage with credential references in normal job data
- secret redaction from logs, samples, AI prompts, and error payloads
- outbound host allowlists and protection against SSRF and DNS rebinding
- blocking of loopback, link-local, metadata, and private-network targets unless explicitly enabled for a trusted self-hosted deployment
- bounded redirects, timeouts, response sizes, pages, and item counts
- per-source concurrency and rate limits
- read-only HTTP methods by default
- an audit trail for source creation, discovery, mapping approval, dry runs, and imports

Connector definitions and samples may contain commercially sensitive catalog information. AI provider routing and retention policy must therefore follow the same provider-profile and audit rules as other AI workflows.

## Proposed Domain And Job Model

Target concepts:

- `ImportSource`: source type, base location, authorization state, secret reference, and operational limits
- `ImportConnectorDefinition`: versioned, approved endpoints, mapping, transformations, and pagination behavior
- `ImportBatch`: discovery or dry-run snapshot, counts, status, and approval state
- `ImportCandidate`: normalized staged item with source lineage, validation issues, and proposed action
- `ImportEntityLink`: stable link from a source object to the resulting ProjectPIM entity
- `IntegrationJob`: existing background execution record used to apply an approved batch

Large source payloads and media should use bounded blob/object storage rather than unbounded JSON columns in operational tables. Retention rules should remove expired raw samples without removing required audit and lineage records.

## Layer Ownership

- `Platform.Domain`: import workflow entities and invariants only
- `Platform.Application`: discovery/import use cases, source-adapter contracts, staging, validation, approval, and application-service coordination
- `Platform.Infrastructure`: HTTP clients, OpenAPI parsing, website crawling/rendering, secret storage, persistence, and concrete adapters
- `Platform.Api`: thin admin endpoints for sources, discovery, mappings, batches, approvals, and jobs
- `Platform.Worker`: bounded discovery and import-job execution
- `Platform.Backoffice`: source setup, mapping review, dry-run diff, issue resolution, approval, and progress views through admin APIs
- `Platform.Contracts`: transport DTOs and validation attributes

## Delivery Plan

### Phase 1: Shared Staging Foundation

- introduce source lineage, import batches, candidates, validation issues, and previews
- reuse the existing integration-job lifecycle for approved execution
- support dry-run and idempotent apply for the current product import format
- add Backoffice summaries and per-item error visibility

### Phase 2: Conventional REST And OpenAPI

- register REST sources and credential references
- parse OpenAPI or accept representative JSON samples
- support common pagination and bounded read-only discovery
- generate a reviewable connector proposal
- execute only approved connector versions

### Phase 3: Structured Website Discovery

- discover product URLs from feeds and sitemaps
- extract JSON-LD and static structured data
- fetch and stage authorized product media
- add crawl budgets, source authorization, and host safety controls

### Phase 4: Assisted Ambiguity Resolution

- use AI to propose mappings from documentation and samples
- interpret variant and attribute relationships that structured data does not resolve
- show confidence and evidence for operator review
- preserve accepted mapping versions for deterministic reruns

### Phase 5: Incremental Synchronization

- add source cursors, modified-since support, schedules, and change detection
- define per-source conflict and deletion/archive policies
- surface local-versus-source change conflicts for review
- support purpose-built adapters where generic REST behavior is insufficient

## V1 Exit Criteria

- an operator can register a conventional REST source without exposing its secret in job payloads or logs
- discovery produces a bounded sample and a reviewable connector definition
- a dry run stages candidates without changing live catalog data
- validation and deduplication results are visible before approval
- an approved batch imports products, variants, media references/assets, categories, and prices idempotently through existing application services
- rerunning the same source data does not duplicate entities or media
- tests cover source-host restrictions, secret redaction, pagination bounds, mapping-version pinning, validation failures, and idempotent re-application

## Explicit Non-Goals For The First Slice

- unrestricted autonomous browsing or API exploration
- write access to legacy source systems
- guaranteed support for every undocumented API or storefront
- continuous website synchronization
- automatic deletion of local products missing from a later source scan
- importing displayed availability as authoritative inventory
- publishing uncertain AI-derived mappings without operator approval
