# PIM / E-Commerce Engine Architecture Baseline

## Goal

Build a standalone PIM and commerce backend that is:

- API-first
- self-hostable
- suitable for managed cloud/SaaS later
- extensible with custom fields and integrations
- opinionated enough to stay maintainable

This document is a recommended baseline, not a locked specification.

## Recommendation

Use:

- `.NET 10 LTS`
- `ASP.NET Core` for HTTP APIs
- `SQL Server` or `PostgreSQL` as the main relational database
- `OpenAPI` for public/internal API contracts
- `Docker` for local development and deployment
- background processing via `Hangfire`, `Quartz`, or hosted workers
- object storage for media/files

My recommendation is:

- Use `.NET`
- Use `PostgreSQL` from day one when there is no existing production database yet
- Keep the data layer portable enough that provider-specific logic stays isolated

Why:

- `.NET` is a strong fit for domain-heavy business systems
- relational modeling matters a lot for PIM, pricing, markets, inventory, and B2B relationships
- self-hosting is straightforward
- cloud hosting later is also straightforward
- performance is predictable
- the tooling for APIs, auth, migrations, and background jobs is mature

## SQL Server vs PostgreSQL

Both are valid choices here.

Reasons to choose `SQL Server` first:

- you already know it
- faster delivery matters more than theoretical portability
- it is a solid fit for transactional commerce workloads
- .NET integration is excellent

Reasons to choose `PostgreSQL` later or from day one:

- no licensing cost concerns for self-hosted customers
- very strong container/cloud portability
- strong JSON and full-text capabilities
- excellent ecosystem support

Pragmatic decision:

- if there is already a live SQL Server footprint, keep it until migration is justified
- if there is no database yet, default to `PostgreSQL`

## Database Authentication Strategy

Recommended authentication strategy by environment:

- local development: Windows authentication
- self-hosted production: SQL authentication with a dedicated application login
- managed cloud production later: managed identity or token-based authentication where supported

Practical rules:

- do not use a developer's Windows account in production
- do not use `sa` for the application
- give the runtime login only the permissions it needs
- keep migration credentials separate from runtime credentials
- store production secrets in environment variables or a secret manager, not in source-controlled config files

## Architectural Style

Do not build this as a CMS with commerce inside it.

Build it as a standalone commerce platform with these boundaries:

1. `Core API`
   - admin/API used by backoffice, integrations, and automation
2. `Storefront API`
   - read-optimized API for storefront/cart/checkout
3. `Integration API + Webhooks`
   - import/export and event-driven integration points
4. `Background Workers`
   - imports, exports, price recalculation, search indexing, event delivery
5. `Backoffice UI`
   - separate frontend consuming the same API

This gives you:

- clean separation from CMS
- support for multiple frontends
- easier self-hosting
- easier evolution into SaaS
- an admin surface for catalog, pricing, customers, companies, markets, and orders

## Monolith First, Modular Internally

Do not start with microservices.

Start with a modular monolith:

- one deployable backend
- one database
- internal modules with clear boundaries

Suggested modules:

- `Identity`
- `Backoffice`
- `Customers`
- `Companies`
- `Catalog`
- `Pricing`
- `Inventory`
- `Markets`
- `Cart`
- `Orders`
- `Checkout`
- `Custom Fields`
- `Integrations`
- `Search`
- `Media`

This is the safest way to keep complexity under control while still allowing later extraction if one area grows large.

## Tenant Model

You mentioned both self-hosting and cloud.

Recommended approach:

- Self-hosted: single tenant per installation
- Cloud/SaaS later: logical multi-tenancy in application and database schema

Do not start with hard multi-tenancy if you do not need it immediately.

Instead:

- design entities so a tenant boundary can be introduced later
- keep tenant-aware abstractions in the code
- avoid assumptions that IDs are globally unique without context

## Core Domain Model

## Customers and Companies

You need both B2C and B2B support.

Recommended entities:

- `Customer`
  - person/contact
  - login identity may be linked here
- `Company`
  - legal/business entity
- `CompanyMembership`
  - connects customer to company
  - supports roles and permissions

`CompanyMembership` should include fields like:

- company id
- customer id
- role
- status
- default buyer flag
- permission set
- valid from / valid to

This is better than a simple direct link because B2B setups usually need:

- multiple contacts per company
- one contact tied to multiple companies
- buyer/approver/admin roles
- future delegation and approval flows

## Products

Your current instinct, `base product + variant`, is still the right default.

Recommended entities:

- `Product`
  - conceptual sellable family or parent
- `Variant`
  - concrete sellable SKU
- `ProductAttribute`
  - definitions such as color, size, material
- `ProductAttributeValue`
  - normalized attribute values
- `Category`
  - classification/navigation
- `Brand` or equivalent optional dimension
- `ProductStatusDefinition`
  - admin-defined catalog status with buyability rules
- `MediaAsset`
  - images, documents, videos

Recommended rule:

- stock, price, and cart lines should normally point to `Variant`
- `Product` is used for shared content and grouping
- buyability should be resolved from explicit status definitions, not ad hoc booleans

This avoids a lot of ambiguity later.

### Product Base Fields

At minimum, include:

- internal id
- external id
- sku
- ean / gtin
- mpn
- name
- slug
- status
- product type
- tax category
- unit of measure
- status definition
- weight / dimensions
- primary image
- created at
- updated at

For variants:

- sku
- ean / gtin
- attribute combination
- status definition
- primary image override optional

## Product Statuses and Buyability

Do not hardcode product availability into a single built-in enum if you want flexibility for admins.

Recommended approach:

- keep a `ProductStatusDefinition` table
- let admins create statuses such as `Draft`, `Ready`, `Discontinued`, `ComingSoon`
- let each status carry rules such as:
  - visible in backoffice
  - visible in storefront
  - buyable
  - searchable

Resolved buyability should depend on:

- product status definition
- variant status definition
- market availability
- effective price availability
- effective inventory availability or backorder rule

This gives you flexibility without making checkout rules ambiguous.

## Markets

Markets should be first-class.

Recommended `Market` fields:

- id
- code
- name
- default currency
- supported currencies
- default culture
- supported cultures
- vat mode
- base price list
- active flag

Markets should control:

- which price lists are available
- which inventory locations are available
- which products/assortments are available
- which cultures are available
- payment/shipping methods

Avoid scattering market rules across unrelated modules.

## Price Lists

Start simple, but model them cleanly.

Recommended entities:

- `PriceList`
- `PriceListEntry`

Fields:

- price list id
- target type: product, variant, category, company, customer group
- target id
- currency
- amount
- vat included flag
- min quantity
- valid from / valid to

You can start with almost no pricing engine logic and still support:

- market-specific pricing
- B2B pricing
- campaign windows
- VAT included/excluded behavior

Do not hardcode VAT directly into product records.

## Inventory

Recommended entities:

- `InventoryLocation`
- `InventoryBalance`
- `InventoryReservation`
- `InventoryTransaction`

Why reservations matter:

- carts and checkout need temporary stock handling
- order placement should convert reservation into allocation/deduction

Fields should support:

- location
- variant
- on hand
- reserved
- available
- backorderable
- lead time

## Cultures and Translations

Do not duplicate full entities per culture.

Use translatable field storage.

Recommended pattern:

- core invariant fields in main entity table
- translated fields in translation tables

Examples:

- `ProductTranslation`
- `CategoryTranslation`
- `BrandTranslation`

Translated fields typically include:

- name
- short description
- long description
- SEO title
- SEO description
- custom translatable field values

## Custom Fields / Extensibility

This is a major requirement, so it needs a proper model from day one.

Recommended approach:

- typed field definitions
- scoped by entity type
- optionally scoped by market/culture
- values stored separately from base entity columns

Core entities that should support custom fields:

- customer
- company
- product
- variant
- cart
- order

Suggested metadata for a field definition:

- key
- label
- entity type
- data type
- required flag
- localized flag
- market scoped flag
- validation rules
- default value
- indexing/searchability flag

Data types:

- text
- long text
- number
- boolean
- decimal
- date/time
- enum
- multi-select
- json
- relation

Important design rule:

- keep critical commerce fields as real columns
- use custom fields for extension, not for everything

If everything becomes dynamic, reporting, indexing, filtering, and performance will degrade quickly.

## AI-Assisted Content

AI is a good fit here, but it should be assistive rather than authoritative.

Good use cases:

- autogenerated product descriptions
- translation suggestions
- SEO title/description suggestions
- attribute normalization suggestions

Do not model this as only a single `AI enabled` boolean on a field.

Better approach:

- each field definition can carry an AI capability setting
- examples:
  - `None`
  - `Generate`
  - `Translate`
  - `Rewrite`
  - `Summarize`
  - combinations later if needed

This keeps the model useful when different fields need different AI behaviors.

Recommended rule:

- AI writes suggestions or drafts
- admins review and publish
- published customer-facing fields remain normal domain data

Do not make the AI system the source of truth for catalog text.

Operationally, this fits best as:

- backoffice actions that trigger generation
- background jobs that call an AI provider
- generated output stored as draft content or suggestions
- audit trail of who accepted or rejected the suggestion

## Cart

Treat cart as a proper domain object, not a temporary session blob.

Recommended entities:

- `Cart`
- `CartLine`
- `CartAddress`
- `CartDiscount` later if needed

`Cart` should support:

- anonymous or authenticated ownership
- customer/company linkage
- market
- currency
- culture
- price context
- shipping selections
- payment selection
- custom fields

This gives you room for B2B flows, saved carts, quotes, and approval processes later.

## Purchase / Checkout

You mentioned some type of purchase solution. I would separate:

- `Order`
- `Payment`
- `Checkout Provider`

Recommended rule:

- your platform owns cart, order, and order state
- payment providers are external adapters

Examples of adapters later:

- Stripe
- Adyen
- Klarna
- invoice providers

Suggested entities:

- `Order`
- `OrderLine`
- `OrderAddress`
- `PaymentTransaction`
- `Shipment` later

Do not let the external payment provider become the source of truth for order structure.

## API Strategy

Expose two API surfaces:

1. Admin/Integration API
   - full CRUD
   - bulk operations
   - webhooks
   - import/export support
2. Storefront API
   - optimized reads
   - product browsing
   - pricing resolution
   - inventory availability
   - cart and checkout

Use:

- REST first
- webhooks for outbound integrations
- async jobs for heavy imports/exports

GraphQL can be added later, but REST is the right starting point for operational simplicity.

## Agentic Shopping

If you want to support "agentic shopping first", treat that as a commerce-platform concern with a CMS companion, not as a CMS-only initiative.

The platform should own:

- structured catalog facts
- variant-level price and availability resolution
- machine-readable buyability rules
- customer/company permission context
- cart/order actions with deterministic side effects
- auditability and idempotency

The CMS or presentation layer should own:

- editorial narratives
- campaign landing pages
- merchandising copy
- presentation-specific composition

Practical implication:

- do not contort the admin PIM into a chatbot product
- do make the storefront/cart/order APIs explicit enough that a shopping agent can reason over them without scraping UI-oriented output

## Integrations

You explicitly want integrations both to and from the platform.

Support both:

- pull integrations via APIs
- push integrations via webhooks/events

Recommended capabilities:

- API keys / OAuth client credentials
- webhooks with retries and signatures
- import/export jobs
- idempotency keys for write APIs
- correlation ids for tracing
- audit log for external writes

Common integration targets:

- ERP
- CMS
- DAM
- search/indexing systems
- payment providers
- shipping providers
- BI/reporting

## Search

Do not make the main SQL database handle advanced catalog search alone.

Recommended:

- SQL remains source of truth
- search index is projection/read model

Start with either:

- PostgreSQL full text if needs are basic
- Elasticsearch / OpenSearch / Meilisearch if catalog search becomes central

## Auth and Permissions

You likely need three permission layers:

1. Platform admins
2. API/integration clients
3. Customer/company users

Recommended:

- OpenID Connect / OAuth 2.1 compatible auth
- role + policy-based permissions
- company-scoped permissions for B2B contacts

Examples:

- customer can manage own carts/orders
- company admin can manage company users and company carts
- purchaser can place orders
- approver can approve but not administer

## Data and Event Model

Use:

- transactional SQL writes
- domain events internally
- integration events externally

Recommended supporting patterns:

- outbox table for reliable event publishing
- audit trail on important entities
- soft delete where business recovery matters
- optimistic concurrency on mutable business records

This is especially important for:

- prices
- inventory
- carts
- orders
- customer/company relationships

## Hosting Strategy

To support self-hosting and managed cloud, design deployment around containers.

Recommended deployment units:

- API container
- worker container
- database
- optional search
- optional cache
- object storage

For self-hosting:

- Docker Compose should work well

For managed cloud later:

- same containers on Azure Container Apps, AKS, AWS ECS, or Kubernetes

Avoid hosting assumptions tied to one cloud provider in your core design.

## Suggested First Version Scope

Do not try to build all commerce features at once.

Build v1 around these modules:

1. identity/auth
2. customers and companies
3. company memberships and roles
4. products and variants
5. categories
6. product translations
7. custom fields
8. price lists
9. inventory locations and balances
10. markets
11. cart
12. order creation from cart
13. payment adapter abstraction
14. admin/integration API
15. webhooks

Delay until later:

- campaigns/promotions engine
- advanced approval workflows
- returns/RMA
- subscriptions
- advanced shipping
- advanced search merchandising
- CMS features

## Suggested Solution Structure

One practical starting structure:

```text
/src
  /Platform.Api
  /Platform.StorefrontApi
  /Platform.Backoffice
  /Platform.Worker
  /Platform.Domain
  /Platform.Application
  /Platform.Infrastructure
  /Platform.Contracts
  /Platform.Migrations
/deploy
  /docker
  /k8s
/docs
```

Alternative:

- keep a single API project initially and split only when pressure appears

That is also a valid choice.

## Technology Choices I Would Make

If I were starting this from zero, I would choose:

- `.NET 10 LTS`
- `ASP.NET Core`
- `PostgreSQL` for a new install with no existing database footprint
- `Entity Framework Core` or `Dapper + EF Core` hybrid
- `FluentValidation`
- `OpenAPI/Swagger`
- `Docker Compose`
- `Redis` only if caching/session/event workloads justify it
- `OpenSearch` or `Meilisearch` later, not on day one

## Risks to Control Early

The biggest design risks are:

1. making custom fields too dynamic
2. mixing market logic everywhere
3. letting product vs variant responsibilities stay unclear
4. making price/inventory rules implicit instead of explicit
5. trying to build CMS, PIM, ERP, and commerce in one pass
6. starting with microservices too early

## My Direct Recommendation

Your core instinct is sound:

- standalone backend
- API-first
- .NET
- SQL database
- support for self-hosting

I would not replace that direction.

What I would tighten is:

- modular monolith first
- product + variant model
- explicit company membership model
- first-class markets
- custom fields as controlled extensibility
- carts/orders/payments kept in your own domain

## Recommended Next Step

The best next artifact is not code yet. It is a more precise domain specification.

Write next:

1. bounded contexts/modules
2. core entity list with required fields
3. rules for product vs variant
4. rules for price resolution
5. rules for inventory resolution
6. market/culture behavior
7. cart-to-order flow
8. permission model

After that, it makes sense to generate the initial `.NET` solution and database schema.
