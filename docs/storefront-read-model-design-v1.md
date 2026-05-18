# Storefront Read Model Design v1

## Purpose

This document defines the next implementation step for `Platform.StorefrontApi` after the first on-the-fly browse/detail slice.

It explains how to move from:

- runtime composition over admin/domain-shaped entities

to:

- a purpose-built storefront projection optimized for CMS and storefront reads

This is the implementation-oriented companion to:

- [storefront-api-contract-v1.md](./storefront-api-contract-v1.md)
- [storefront-api-usage.md](./storefront-api-usage.md)

## Implementation Status

Current code status:

- the first projection foundation is now implemented
- `StorefrontProductProjection` exists in the codebase
- EF configuration and migration exist
- repository, builder, and refresh-service foundations exist
- storefront projection rebuild can be executed through the admin/worker job pipeline
- storefront product browse/detail now read through the projection repository

That means this document is now both:

- the design rationale
- the implementation tracking note for the remaining projection work

## Why A Read Model Is Needed

The current storefront implementation is acceptable for the first slice, but it assembles browse responses at request time by:

- filtering visible products
- loading related brand/category/media data
- resolving variant prices from active price lists
- aggregating availability from inventory balances
- building category/brand facets in memory

That is workable for small seeded data and early integration work.

It is not the shape we should keep once:

- the catalog grows
- price sorting becomes important
- facet counts must stay fast and predictable
- storefront traffic increases
- Nexra or other consumers depend on stable response times

The next step is a dedicated storefront read model.

## Design Goals

The storefront read model should be:

- optimized for read queries, not admin writes
- market-aware
- culture-aware
- explicit about price, availability, and buyability
- easy to rebuild
- safe to lag briefly behind write models when eventual consistency is acceptable

It should not:

- replace the core catalog domain model
- become the source of truth for admin writes
- duplicate all admin data blindly
- force the CMS to understand internal persistence rules

## Design Principles

1. `Platform.Api` remains the write/admin surface.
2. `Platform.StorefrontApi` reads from storefront-oriented projections.
3. Projection building happens in `Platform.Infrastructure` and async execution in `Platform.Worker`.
4. Projection data is derived from source-of-truth catalog, pricing, inventory, market, and channel data.
5. Rebuildability matters more than perfect real-time sync in v1.

## Recommended Read Model Shape

Start with one primary projection record per:

- product
- market
- culture
- currency

This gives a localized storefront product document that is cheap to query directly.

Recommended conceptual entity:

- `StorefrontProductProjection`

Recommended storage responsibility:

- `Platform.Infrastructure/Persistence`

Recommended runtime access:

- `Platform.Application/Storefront`
- `Platform.Infrastructure/Storefront`

## Recommended Projection Fields

The first projection should include fields needed by current and near-term storefront reads.

### Identity And Routing

- `Id`
- `ProductId`
- `MarketId`
- `MarketCode`
- `CultureCode`
- `ProductNumber`
- `Slug`
- `ProductType`

### Display Content

- `Name`
- `ShortDescription`
- `LongDescription`
- `SeoTitle`
- `SeoDescription`

### Brand

- `BrandId`
- `BrandCode`
- `BrandName`
- `BrandSlug`
- `BrandWebsiteUrl`
- `BrandLogoUrl`

### Category Data

- `CategoryIds`
- `CategoryCodes`
- `CategorySlugs`
- `CategoryNames`
- optionally one denormalized category breadcrumb/ancestor representation for filtering

### Media

- `PrimaryImageUrl`
- `MediaJson` or a normalized child table for storefront media items

### Commerce State

- `HasVariants`
- `IsVisible`
- `IsBuyable`
- `BuyabilityReasonsJson`
- `AvailabilityStatus`
- `AvailableQuantity`
- `IsBackorderable`

### Price State

- `CurrencyCode`
- `PriceAmount`
- `CompareAtAmount`
- `VatIncluded`
- `PriceListCode`
- optionally `MinVariantPriceAmount`
- optionally `MaxVariantPriceAmount`

### Variant State

For v1, store variant detail in a denormalized payload or separate child projection rows.

Recommended choices:

1. `VariantJson` for the first projection pass
2. later `StorefrontVariantProjection` if variant filtering/sorting becomes first-class

Variant payload should include:

- `VariantId`
- `Sku`
- `Ean`
- `Mpn`
- `Barcode`
- `IsDefaultVariant`
- `PrimaryImageUrl`
- `Price`
- `Availability`
- `Buyability`
- flattened attribute values

### Search And Filter Fields

- `SearchText`
- `SortName`
- `SortProductNumber`
- `SortPriceAmount`
- `BrandSortName`
- filterable category codes/slugs
- filterable brand code
- optionally flattened attribute filters for future faceting

### Synchronization And Diagnostics

- `SourceUpdatedAtUtc`
- `ProjectedAtUtc`
- `ProjectionVersion`

## Storage Options

There are two reasonable v1 storage shapes.

### Option A: Projection Table In PostgreSQL

Recommended first choice.

Examples:

- `storefront_product_projections`
- optional `storefront_projection_runs`

Advantages:

- fits current architecture
- easy to query with EF Core
- easy to rebuild from existing relational source data
- no new infrastructure dependency

Disadvantages:

- less flexible than a dedicated search engine for advanced search later

### Option B: Search-Oriented Document Store Or External Search Index

Not recommended as the first implementation step.

Use only when we actually need:

- full-text ranking
- typo tolerance
- advanced merchandising rules
- larger-scale discovery workloads

For now, PostgreSQL projection tables are the pragmatic path.

## Recommended Query Model

The storefront API should stop resolving browse state by traversing many write-side relations in request handlers.

Instead:

- `GET /api/storefront/products` should query projection rows directly
- `GET /api/storefront/products/{slug}` should query one projection row directly
- `GET /api/storefront/products/by-number/{productNumber}` should query one projection row directly

The projection query service should support:

- filtering by `marketCode`
- filtering by `cultureCode`
- filtering by `categorySlug`
- filtering by `brandCode`
- filtering by text query
- sorting by `productNumber`
- sorting by `name`
- sorting by projected `price`
- paging
- category/brand facet counts

## Facet Strategy

Facet counts should be calculated over the projection query, not reconstructed from the write model at request time.

Recommended v1 facet scope:

- category counts
- brand counts

Recommended behavior:

- counts respect market visibility
- counts respect current text query
- category facets respect selected brand
- brand facets respect selected category tree

Later additions can include:

- attribute facets
- price ranges
- availability filters

## Price Resolution Strategy

Price should be resolved during projection, not on every browse request.

Recommended rule for the first projection:

- compute the effective storefront price for each visible sellable product in a given market/currency context
- store top-level product price as the lowest visible buyable variant price
- also store per-variant resolved price in the variant payload

That makes:

- product price sort cheap
- product cards cheap
- detail responses consistent with browse

## Availability Resolution Strategy

Availability should also be projected.

Recommended rule:

- aggregate inventory only from inventory locations assigned to the market
- compute variant availability first
- compute product availability from visible variants

Recommended stored fields:

- product-level `AvailabilityStatus`
- product-level `AvailableQuantity`
- product-level `IsBackorderable`
- variant-level availability in variant payload

## Buyability Resolution Strategy

Buyability should be projected as structured state, not recalculated in every controller call.

Recommended fields:

- `IsVisible`
- `IsBuyable`
- `BuyabilityReasonsJson`

Reasons should remain machine-readable, for example:

- `NotVisibleInMarket`
- `ProductStatusNotBuyable`
- `VariantStatusNotBuyable`
- `MissingPrice`
- `OutOfStock`
- `Unavailable`
- `NoBuyableVariants`

This keeps Nexra and future shopping-agent consumers on explicit contracts.

## Projection Ownership

### Source Modules

Projection input comes from:

- catalog products
- variants
- brands
- categories
- media
- markets and market product assignments
- price lists and entries
- inventory locations and balances

### Owning Runtime Module

The storefront projection runtime should live in:

- `Platform.Application/Storefront`
- `Platform.Infrastructure/Storefront`

The worker orchestration should live in:

- `Platform.Worker`

The storefront API should remain a thin consumer of the projection query service.

## Update Model

Use two update modes.

### 1. Full Rebuild

Needed for:

- first deployment
- schema changes
- recovery from projection bugs

Recommended command surface:

- persisted integration or projection rebuild job

### 2. Incremental Refresh

Needed for normal operations.

Recommended trigger sources:

- existing outbox events for product and brand changes
- additional events for:
  - category changes
  - market product assignment changes
  - price list entry changes
  - variant changes
  - inventory balance changes

Recommended pattern:

1. write-side change emits outbox event
2. worker consumes the event
3. worker determines affected `productId` values
4. worker rebuilds affected projection rows for impacted markets/cultures

## Event Coverage Needed

The current outbox foundation is a good start, but storefront projection maintenance will require broader event coverage.

At minimum, projection refresh should react to:

- `ProductCreated`
- `ProductUpdated`
- `ProductTranslationUpdated`
- `VariantCreated`
- `VariantUpdated`
- `VariantStatusChanged`
- `BrandUpdated`
- `CategoryUpdated`
- `MarketProductAssignmentChanged`
- `PriceListUpdated`
- `PriceListEntryUpdated`
- `InventoryBalanceUpdated`
- `InventoryLocationMarketAssignmentChanged`

Some of these may map to a smaller internal event set if the payload includes enough affected IDs.

## Recommended Persistence Model

The simplest first persistence model is:

### Table: `StorefrontProductProjections`

Suggested keys:

- primary key: `Id`
- unique key: `MarketCode + CultureCode + CurrencyCode + ProductNumber`
- unique key: `MarketCode + CultureCode + CurrencyCode + Slug`

Suggested indexed fields:

- `MarketCode`
- `CultureCode`
- `BrandCode`
- `ProductNumber`
- `Slug`
- `IsVisible`
- `IsBuyable`
- `AvailabilityStatus`
- `PriceAmount`
- `SortName`

If JSON columns are used for media, categories, attributes, or variants, keep the main filter/sort fields duplicated in scalar columns so browse remains cheap.

## Repository And Service Design

Recommended new abstractions:

```csharp
public interface IStorefrontProductProjectionRepository
{
    Task<PagedProjectionResult> ListAsync(StorefrontProjectionQuery query, CancellationToken cancellationToken);
    Task<StorefrontProductProjection?> GetBySlugAsync(string marketCode, string cultureCode, string slug, CancellationToken cancellationToken);
    Task<StorefrontProductProjection?> GetByProductNumberAsync(string marketCode, string cultureCode, string productNumber, CancellationToken cancellationToken);
    Task UpsertAsync(StorefrontProductProjection projection, CancellationToken cancellationToken);
    Task DeleteByProductIdAsync(Guid productId, CancellationToken cancellationToken);
}

public interface IStorefrontProjectionBuilder
{
    Task<IReadOnlyList<StorefrontProductProjection>> BuildForProductAsync(Guid productId, CancellationToken cancellationToken);
}
```

Recommended worker-facing abstraction:

```csharp
public interface IStorefrontProjectionRefreshService
{
    Task RefreshProductAsync(Guid productId, CancellationToken cancellationToken);
    Task RefreshProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task RebuildAllAsync(CancellationToken cancellationToken);
}
```

## Relationship To Current Endpoints

This design does not require changing the public storefront contract first.

Recommended migration path:

1. keep current endpoints and response shapes
2. introduce projection persistence and builder
3. switch product browse/detail queries to the projection repository
4. keep controllers unchanged
5. add `price` sorting after projection is in place

This avoids forcing external consumers to absorb a contract break just because the internals improve.

## Phased Implementation Plan

### Phase 1: Projection Foundations

- add projection entity and EF configuration
- add projection repository
- add projection builder
- add full rebuild job path

Exit:

- projection table can be rebuilt from current source-of-truth data

Current implementation status:

- projection entity, repository, builder, refresh service, and migration are done
- worker-hosted rebuild orchestration is done through a real integration job

### Phase 2: Product Query Cutover

- switch storefront product browse/detail queries to projection reads
- keep current API shape stable
- add projected `price` sort

Exit:

- `GET /api/storefront/products`
- `GET /api/storefront/products/{slug}`
- `GET /api/storefront/products/by-number/{productNumber}`

all read from projection storage

Current implementation status:

- the storefront product endpoints now read from `StorefrontProductProjection`
- the external storefront contract stayed stable during the cutover
- projected `price` sorting is still deferred

### Phase 3: Incremental Refresh

- extend outbox event coverage
- add worker-driven targeted refresh

Exit:

- normal write operations keep projection reasonably fresh without full rebuilds

Current implementation status:

- partially implemented
- direct product, variant, brand, category subtree, market product assignment, inventory-location market assignment, price-list entry, and inventory-balance mutations now enqueue internal storefront projection refresh requests
- storefront refresh requests are processed as internal outbox messages with event-type-specific polling, separate from external webhook fanout
- storefront product reads do not rebuild projections on read misses; rebuilds and incremental refreshes are worker/admin responsibilities
- `Platform.Worker` processes refresh requests before publishing outbox messages, resolving affected variants back to product projection rows
- refresh processing coalesces each batch into distinct product ids before rebuilding projections, marks invalid refresh payloads complete after logging a warning, and logs message/product counts for observability
- the documented direct dependency fan-outs for Phase 3 are now covered; remaining hardening is operational monitoring and any future retry policy beyond outbox reprocessing

### Phase 4: Search Refinement

- add better text search over projection fields
- add additional facets or price ranges if needed
- decide whether PostgreSQL remains sufficient or whether a separate search projection is justified

## Non-Goals For This Design

This design does not yet include:

- carts
- checkout
- customer-specific pricing
- customer-specific availability
- recommendation ranking
- CMS page composition
- external search engine adoption

Those may build on the same projection later, but they are not required for the first read-model cutover.

## Risks And Tradeoffs

### 1. Eventual Consistency

The storefront view may lag behind admin writes briefly.

That is acceptable if:

- rebuilds are deterministic
- refresh delays are small
- operators have rebuild tooling

### 2. Projection Drift

If refresh coverage misses a write path, storefront data can become stale.

Mitigation:

- explicit event coverage review
- rebuild job
- tests around projection refresh triggers

### 3. Over-Denormalization

Packing too much into one row can make projection writes heavier.

Mitigation:

- start with product-level projection plus variant payload
- split variant projection only if needed

### 4. Premature Search Complexity

Introducing an external search engine too early would slow delivery.

Mitigation:

- use PostgreSQL projection tables first
- revisit only when search requirements justify it

## Recommended Next Step

The next useful implementation artifact after this document is:

1. add `StorefrontProductProjection` design to the codebase
2. add automatic targeted projection refresh from write-side events
3. add projected `price` sorting and richer browse filters on top of the new read model
4. then revisit whether PostgreSQL projection queries remain sufficient before introducing a dedicated search engine
