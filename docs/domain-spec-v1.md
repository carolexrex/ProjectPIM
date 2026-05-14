# Domain Specification v1

## Purpose

This document turns the baseline architecture into a concrete first domain model for the platform.

The goal is to define:

- bounded contexts/modules
- core entities
- required fields
- key relationships
- business rules
- what belongs in v1 and what does not

This is the specification that should drive:

- database schema design
- API contract design
- application service boundaries
- event contracts

## Platform Scope

The platform is a standalone PIM and commerce engine with:

- admin and integration APIs
- a backoffice for administrators
- storefront/cart/checkout APIs
- support for B2C and B2B
- product and variant catalog modeling
- market-specific pricing and inventory
- translations
- custom fields
- inbound and outbound integrations

The platform is not, in v1:

- a CMS
- a full ERP
- a promotion engine
- a warehouse management system

## Design Principles

1. Critical commerce data uses explicit columns and tables.
2. Custom fields extend the model but do not replace the model.
3. `Variant` is the main sellable unit for price, stock, and cart lines.
4. `Market` is first-class and controls availability context.
5. The platform owns cart and order state.
6. External systems integrate through APIs, jobs, and events.
7. Backoffice is a first-class product surface.
8. AI-generated content should be reviewed before publication.

## Cross-Cutting Conventions

## Identity and IDs

All primary entities should have:

- `Id`
- `TenantId` reserved for future SaaS support, nullable in self-hosted v1 if desired
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `RowVersion` or equivalent optimistic concurrency token

Recommended ID type:

- `Guid` for public/domain entity identity in v1

Recommended external reference pattern:

- `ExternalId` optional but indexed

This makes ERP/DAM/CMS integration easier without exposing internal keys.

## Status Fields

Prefer explicit statuses over booleans when state can grow.

Examples:

- `Draft`, `Active`, `Archived`
- `Pending`, `Approved`, `Rejected`
- `Open`, `Converted`, `Expired`, `Cancelled`

## Soft Delete

Use soft delete where historical integrity matters:

- customers
- companies
- products
- variants
- price lists
- inventory locations

Avoid hard delete if the entity may be referenced by orders, logs, or external IDs.

## Custom Field Strategy

Custom fields are supported on selected entity types through:

- field definitions
- field assignments/scoping
- field values

Field definitions must support:

- entity type
- key
- label
- data type
- required flag
- localized flag
- market scoped flag
- validation rules
- default value
- searchability flag
- AI capability
- active flag

Field values must support:

- typed storage or validated JSON storage
- entity id
- field definition id
- optional culture
- optional market id

Critical fields must not be implemented as custom fields:

- product SKU
- product EAN/GTIN
- market code
- price amounts
- inventory quantities
- cart/order totals

## AI-Assisted Field Workflow

AI should support content workflows, not replace the domain model.

Recommended use cases:

- product description generation
- translation suggestions
- SEO text generation
- enrichment suggestions for structured attributes

Recommended workflow:

1. An admin triggers generation from backoffice or bulk jobs.
2. The platform sends relevant context to an AI provider.
3. The generated result is stored as a suggestion or draft.
4. An admin accepts, edits, or rejects the suggestion.
5. Only accepted content is copied into the live translated fields.

Important rules:

1. AI output should never overwrite published content automatically in v1.
2. Suggestions should be tied to entity, field, culture, and market where relevant.
3. Prompt templates and provider settings should be configurable later, not hardcoded forever.
4. Translation generation should respect market/culture rules already defined in the platform.
5. AI behavior should be field-configurable through capability metadata, not only an on/off flag.

Recommended field-level AI capability values:

- `None`
- `Generate`
- `Translate`
- `Rewrite`
- `Summarize`

Implementation note:

- custom fields can store this metadata in the database
- built-in fields should expose the same metadata through an application field registry in v1

Examples:

- `ProductTranslation.LongDescription`: `Generate`, `Rewrite`, `Translate`
- `ProductTranslation.SeoTitle`: `Generate`, `Rewrite`, `Translate`
- `ProductTranslation.Name`: usually `Translate` only
- `Variant.Ean`: `None`
- `PriceListEntry.Amount`: `None`

## Bounded Contexts

## 1. Identity

Responsible for:

- platform users
- customer authentication linkage
- API clients
- roles and policies

Core entities:

- `User`
- `Role`
- `ApiClient`
- `RefreshToken` or equivalent auth persistence if needed

Notes:

- `Customer` is a business entity in the customer module, not the same thing as `User`
- one `Customer` may be linked to one `User` account in v1

## 2. Customers

Responsible for:

- personal customer records
- addresses
- preferences
- company memberships

Core entities:

- `Customer`
- `CustomerAddress`
- `CustomerGroup`

## 3. Companies

Responsible for:

- B2B organizations
- company contacts/relationships
- billing settings
- memberships and permissions

Core entities:

- `Company`
- `CompanyAddress`
- `CompanyMembership`

## 4. Catalog

Responsible for:

- products
- variants
- attributes
- categories
- brands
- product status definitions
- product media
- translations

Core entities:

- `Product`
- `ProductTranslation`
- `Variant`
- `VariantTranslation` optional in v1
- `ProductAttribute`
- `AttributeOption`
- `VariantAttributeValue`
- `Category`
- `CategoryTranslation`
- `Brand`
- `BrandTranslation`
- `ProductStatusDefinition`
- `ProductCategory`
- `ProductMedia`

## 5. Markets

Responsible for:

- market definitions
- currencies
- cultures
- assortment availability
- market-level defaults

Core entities:

- `Market`
- `MarketCurrency`
- `MarketCulture`
- `MarketCatalogAssignment`
- `MarketInventoryLocation`
- `MarketPriceList`

## 6. Pricing

Responsible for:

- price lists
- price entries
- price resolution rules
- VAT inclusion behavior

Core entities:

- `PriceList`
- `PriceListEntry`

## 7. Inventory

Responsible for:

- inventory locations
- stock balances
- reservations
- stock transactions

Core entities:

- `InventoryLocation`
- `InventoryBalance`
- `InventoryReservation`
- `InventoryTransaction`

## 8. Cart

Responsible for:

- carts
- cart lines
- cart addresses
- cart custom fields
- checkout preparation

Core entities:

- `Cart`
- `CartLine`
- `CartAddress`

## 9. Orders

Responsible for:

- order creation from cart
- order state
- payment transaction linkage
- order history

Core entities:

- `Order`
- `OrderLine`
- `OrderAddress`
- `OrderStatusHistory`
- `PaymentTransaction`

## 10. Integrations

Responsible for:

- import/export jobs
- webhook subscriptions
- integration logs
- idempotency and correlation

Core entities:

- `IntegrationJob`
- `WebhookSubscription`
- `WebhookDelivery`
- `InboundRequestLog`
- `OutboxMessage`

## Core Entity Specifications

## Customer

Represents a person/contact that can authenticate, own carts, and place orders.

Required fields:

- `Id`
- `ExternalId` nullable
- `UserId` nullable
- `Email`
- `NormalizedEmail`
- `FirstName`
- `LastName`
- `Phone` nullable
- `PreferredCulture` nullable
- `DefaultMarketId` nullable
- `Status`
- `IsGuest`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Rules:

1. `Email` should be unique per tenant for non-guest customers.
2. A guest customer may later be converted into a registered customer.
3. A customer may be linked to zero or many companies through `CompanyMembership`.

## CustomerAddress

Required fields:

- `Id`
- `CustomerId`
- `Type`
- `Attention` nullable
- `FirstName`
- `LastName`
- `CompanyName` nullable
- `Line1`
- `Line2` nullable
- `PostalCode`
- `City`
- `Region` nullable
- `CountryCode`
- `Phone` nullable
- `Email` nullable
- `IsDefault`

Rules:

1. A customer can have multiple addresses by type.
2. Default shipping and billing behavior should be resolved in application logic, not by assuming one address total.

## Company

Represents a legal/business entity for B2B commerce.

Required fields:

- `Id`
- `ExternalId` nullable
- `Code`
- `Name`
- `LegalName` nullable
- `OrganizationNumber` nullable
- `VatNumber` nullable
- `Email` nullable
- `Phone` nullable
- `DefaultMarketId` nullable
- `DefaultCurrency` nullable
- `Status`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Rules:

1. `Code` must be unique per tenant.
2. A company may have zero or many customer memberships.
3. Company-specific pricing may be attached through price lists in v1.

## CompanyAddress

Required fields:

- `Id`
- `CompanyId`
- `Type`
- `Attention` nullable
- `Line1`
- `Line2` nullable
- `PostalCode`
- `City`
- `Region` nullable
- `CountryCode`
- `Email` nullable
- `Phone` nullable
- `IsDefault`

## CompanyMembership

Links a customer to a company with a role and optional permission override.

Required fields:

- `Id`
- `CompanyId`
- `CustomerId`
- `Role`
- `Status`
- `IsDefaultCompany`
- `CanPlaceOrders`
- `CanApproveOrders`
- `CanManageUsers`
- `ValidFromUtc` nullable
- `ValidToUtc` nullable

Rules:

1. A customer may belong to multiple companies.
2. A company may have multiple customers.
3. Membership validity windows must be respected by checkout and admin actions.
4. At least one active company contact should have admin or user-management capability in real B2B setups, but this can be enforced operationally rather than as a DB constraint.

## Product

Represents the parent product/shared product definition.

Required fields:

- `Id`
- `ExternalId` nullable
- `ProductType`
- `ProductNumber`
- `Slug`
- `BrandId` nullable
- `ProductStatusDefinitionId`
- `TaxCategoryId`
- `UnitOfMeasure`
- `Status`
- `PrimaryImageMediaAssetId` nullable
- `IsVariantParent`
- `HasVariants`
- `Weight` nullable
- `Length` nullable
- `Width` nullable
- `Height` nullable
- `CreatedAtUtc`
- `UpdatedAtUtc`

Rules:

1. `Product` is not assumed to be directly sellable.
2. Shared content belongs on `Product`.
3. Classification, translations, and media are usually attached at `Product` level unless variant-specific override is needed.
4. Buyability must not be resolved from product status text alone; it comes from the linked status definition plus market, price, and stock rules.

## ProductTranslation

Required fields:

- `Id`
- `ProductId`
- `CultureCode`
- `Name`
- `ShortDescription` nullable
- `LongDescription` nullable
- `SeoTitle` nullable
- `SeoDescription` nullable

Rules:

1. One translation row per `ProductId + CultureCode`.
2. Missing translation fallback should be handled by application rules, usually market default culture first.

## Variant

Represents the concrete sellable SKU.

Required fields:

- `Id`
- `ProductId`
- `ExternalId` nullable
- `Sku`
- `Ean` nullable
- `Mpn` nullable
- `Barcode` nullable
- `ProductStatusDefinitionId`
- `Status`
- `PrimaryImageMediaAssetId` nullable
- `IsDefaultVariant`
- `Weight` nullable
- `Length` nullable
- `Width` nullable
- `Height` nullable
- `CreatedAtUtc`
- `UpdatedAtUtc`

Rules:

1. `Sku` must be unique per tenant.
2. All stock, reservations, and cart lines point to `Variant`.
3. All price entries should target `Variant` in v1 unless you explicitly support parent-level fallback.
4. `Product` without variants is allowed only if you choose to model a single implicit variant; the cleaner approach is to always create at least one variant.
5. Variant buyability is gated by both variant and parent product status rules.

Recommended decision for v1:

- Always use a `Variant`, even for simple products with only one purchasable SKU.

That keeps pricing, inventory, and order logic consistent.

## ProductAttribute

Defines selectable or descriptive attribute types.

Required fields:

- `Id`
- `Code`
- `Name`
- `Scope`
- `DataType`
- `IsVariantDefining`
- `IsFilterable`
- `IsRequired`
- `SortOrder`

Rules:

1. Variant-defining attributes are used to distinguish sellable variants such as size and color.
2. Non-variant-defining attributes are descriptive only.

## AttributeOption

Required fields:

- `Id`
- `ProductAttributeId`
- `Code`
- `Value`
- `SortOrder`

## VariantAttributeValue

Required fields:

- `Id`
- `VariantId`
- `ProductAttributeId`
- `AttributeOptionId` nullable
- `ValueText` nullable

Rules:

1. The combination of variant-defining attributes must be unique within a product.

## Category

Required fields:

- `Id`
- `ExternalId` nullable
- `Code`
- `ParentCategoryId` nullable
- `SortOrder`
- `Status`

## CategoryTranslation

Required fields:

- `Id`
- `CategoryId`
- `CultureCode`
- `Name`
- `Slug`
- `Description` nullable

## Brand

Required fields:

- `Id`
- `Code`
- `Status`

## BrandTranslation

Required fields:

- `Id`
- `BrandId`
- `CultureCode`
- `Name`

## ProductStatusDefinition

Defines admin-managed catalog statuses and how they affect storefront behavior.

Required fields:

- `Id`
- `Code`
- `Name`
- `EntityType`
- `IsDefault`
- `IsVisibleInBackoffice`
- `IsVisibleInStorefront`
- `IsBuyable`
- `IsSearchable`
- `SortOrder`
- `Status`

Rules:

1. `EntityType` should support at least `Product` and `Variant`.
2. `Code` must be unique per tenant and entity type.
3. Only active status definitions can be assigned to catalog entities.
4. `IsBuyable = false` must block add-to-cart even if price and stock exist.

## ProductMedia

Required fields:

- `Id`
- `ProductId`
- `MediaAssetId`
- `Type`
- `SortOrder`
- `IsPrimary`
- `CultureCode` nullable
- `MarketId` nullable

Rules:

1. Products must support at least one image in v1.
2. `Product.PrimaryImageMediaAssetId` should act as a fast pointer for common reads.
3. `ProductMedia` stores the ordered gallery and optional market/culture-specific media.

## ProductCategory

Required fields:

- `ProductId`
- `CategoryId`
- `IsPrimary`

Rules:

1. A product can belong to many categories.
2. A primary category is optional but useful for navigation and SEO.

## Market

Represents a sales context such as country, region, or channel-specific market.

Required fields:

- `Id`
- `Code`
- `Name`
- `DefaultCurrency`
- `DefaultCulture`
- `VatMode`
- `Status`

Rules:

1. `Code` must be unique per tenant.
2. A market controls culture, currency, price list, and inventory availability context.
3. A market is not the same as a website; one market may be exposed in multiple channels.

## MarketCurrency

Required fields:

- `Id`
- `MarketId`
- `CurrencyCode`
- `IsDefault`

Rule:

1. Exactly one default currency per market.

## MarketCulture

Required fields:

- `Id`
- `MarketId`
- `CultureCode`
- `IsDefault`

Rule:

1. Exactly one default culture per market.

## MarketCatalogAssignment

Defines which products or assortments are available in a market.

Required fields:

- `Id`
- `MarketId`
- `ProductId`
- `Status`

Rules:

1. Market availability should be explicit.
2. If a product is unavailable in a market, its variants cannot be bought there even if they have price and stock.

## PriceList

Required fields:

- `Id`
- `Name`
- `Code`
- `CurrencyCode`
- `VatIncluded`
- `Status`
- `ValidFromUtc` nullable
- `ValidToUtc` nullable

Rules:

1. `Code` must be unique per tenant.
2. A price list may be assigned to one or many markets.
3. A price list may optionally be targeted to one company in v1 if needed.

## PriceListEntry

Required fields:

- `Id`
- `PriceListId`
- `TargetType`
- `TargetId`
- `MinQuantity`
- `Amount`
- `CompareAtAmount` nullable
- `ValidFromUtc` nullable
- `ValidToUtc` nullable

Recommended v1 constraint:

- `TargetType` should be `Variant`

Possible future target types:

- `Product`
- `Category`
- `Company`
- `CustomerGroup`

Rules:

1. Price resolution must return at most one effective unit price for a cart line.
2. Resolution should prefer more specific assignments over less specific ones.
3. Invalid or expired prices must never resolve.

## InventoryLocation

Required fields:

- `Id`
- `Code`
- `Name`
- `Type`
- `Status`
- `CountryCode` nullable

Rules:

1. `Code` must be unique per tenant.
2. A location may be assigned to one or many markets.

## InventoryBalance

Required fields:

- `Id`
- `InventoryLocationId`
- `VariantId`
- `OnHandQuantity`
- `ReservedQuantity`
- `IncomingQuantity`
- `Backorderable`
- `UpdatedAtUtc`

Derived value:

- `AvailableQuantity = OnHandQuantity - ReservedQuantity`

Rules:

1. Available quantity may be negative only if backorder logic permits it.
2. Balances are the source of truth for stock snapshots.

## InventoryReservation

Required fields:

- `Id`
- `InventoryLocationId`
- `VariantId`
- `CartId` nullable
- `OrderId` nullable
- `Quantity`
- `Status`
- `ExpiresAtUtc` nullable
- `CreatedAtUtc`

Rules:

1. A reservation may be created during cart/checkout.
2. Reservations must expire or be released.
3. On order placement, reservation is converted or consumed by stock transaction logic.

## InventoryTransaction

Required fields:

- `Id`
- `InventoryLocationId`
- `VariantId`
- `Type`
- `QuantityDelta`
- `ReferenceType`
- `ReferenceId`
- `OccurredAtUtc`

Examples of `Type`:

- `Adjustment`
- `Reservation`
- `ReservationRelease`
- `OrderAllocation`
- `Shipment`
- `Return`

## Cart

Represents a draft purchase or quote-like basket.

Required fields:

- `Id`
- `CartNumber`
- `Status`
- `CustomerId` nullable
- `CompanyId` nullable
- `MarketId`
- `CurrencyCode`
- `CultureCode`
- `Email` nullable
- `BillingAddressId` nullable
- `ShippingAddressId` nullable
- `SelectedPaymentMethod` nullable
- `SelectedShippingMethod` nullable
- `PriceListContext` nullable
- `ExpiresAtUtc` nullable
- `CreatedAtUtc`
- `UpdatedAtUtc`

Rules:

1. A cart belongs to one market and one currency at a time.
2. A cart may be anonymous or authenticated.
3. A cart may be company-linked for B2B purchasing.
4. A cart can hold custom fields.

## CartLine

Required fields:

- `Id`
- `CartId`
- `VariantId`
- `Quantity`
- `UnitPrice`
- `VatRate`
- `LineTotal`
- `Comment` nullable

Rules:

1. Quantity must be positive.
2. The line snapshot stores the resolved price used for totals.
3. Repricing can occur when the cart changes or before checkout confirmation.

## CartAddress

Required fields:

- `Id`
- `CartId`
- `Type`
- `FirstName`
- `LastName`
- `CompanyName` nullable
- `Line1`
- `Line2` nullable
- `PostalCode`
- `City`
- `Region` nullable
- `CountryCode`
- `Email` nullable
- `Phone` nullable

## Order

Represents the placed purchase.

Required fields:

- `Id`
- `OrderNumber`
- `Status`
- `CustomerId` nullable
- `CompanyId` nullable
- `MarketId`
- `CurrencyCode`
- `CultureCode`
- `Email`
- `Subtotal`
- `VatTotal`
- `GrandTotal`
- `PlacedAtUtc`
- `PaymentStatus`
- `FulfillmentStatus`

Rules:

1. Order data is an immutable business snapshot except for state transitions and operational references.
2. An order is created from a validated cart.
3. Totals on the order must not rely on current catalog prices after placement.

## OrderLine

Required fields:

- `Id`
- `OrderId`
- `VariantId`
- `Sku`
- `ProductName`
- `VariantDescription` nullable
- `Quantity`
- `UnitPrice`
- `VatRate`
- `LineTotal`

Rules:

1. Order lines keep a snapshot of names, SKU, and price context.
2. Do not depend on live product data when rendering old orders.

## OrderAddress

Required fields:

- `Id`
- `OrderId`
- `Type`
- `FirstName`
- `LastName`
- `CompanyName` nullable
- `Line1`
- `Line2` nullable
- `PostalCode`
- `City`
- `Region` nullable
- `CountryCode`
- `Email` nullable
- `Phone` nullable

## OrderStatusHistory

Required fields:

- `Id`
- `OrderId`
- `FromStatus` nullable
- `ToStatus`
- `ChangedBy`
- `ChangedAtUtc`
- `Comment` nullable

## PaymentTransaction

Required fields:

- `Id`
- `OrderId`
- `Provider`
- `ProviderReference`
- `Type`
- `Status`
- `Amount`
- `CurrencyCode`
- `RequestedAtUtc`
- `CompletedAtUtc` nullable

Rules:

1. Payment providers are adapters.
2. The platform owns final order state.
3. Webhook callbacks from payment providers must be idempotent.

## IntegrationJob

Required fields:

- `Id`
- `Type`
- `Direction`
- `Status`
- `RequestedBy`
- `StartedAtUtc` nullable
- `CompletedAtUtc` nullable
- `PayloadReference` nullable
- `ResultSummary` nullable

## WebhookSubscription

Required fields:

- `Id`
- `Name`
- `EndpointUrl`
- `Secret`
- `IsActive`
- `EventTypes`

## WebhookDelivery

Required fields:

- `Id`
- `WebhookSubscriptionId`
- `EventId`
- `Status`
- `AttemptCount`
- `LastAttemptAtUtc` nullable
- `NextAttemptAtUtc` nullable
- `ResponseCode` nullable

## OutboxMessage

Required fields:

- `Id`
- `EventType`
- `AggregateType`
- `AggregateId`
- `OccurredAtUtc`
- `Payload`
- `PublishedAtUtc` nullable

## Main Business Rules

## Product and Variant Rules

1. `Variant` is the purchasable record.
2. `Product` groups variants and shared content.
3. Inventory, price, cart lines, and order lines resolve from `Variant`.
4. A simple product still gets one variant in v1.
5. Product and variant statuses are admin-managed definitions, not fixed enums.
6. A variant is buyable only if:
   - the product status allows buyability
   - the variant status allows buyability
   - the product is available in the market
   - an effective price can be resolved
   - stock/backorder rules allow purchase

## Market Rules

1. Every cart and order belongs to exactly one market.
2. Products must be explicitly available in the market.
3. Price lists available to the market define price resolution input.
4. Inventory locations available to the market define stock resolution input.
5. Cultures and currencies available to the market constrain storefront behavior.

## Price Resolution Rules

Minimum v1 resolution inputs:

- market
- currency
- variant
- quantity
- company optional
- current timestamp

Minimum v1 resolution order:

1. company-specific market price
2. market base price
3. no price found

Rules:

1. Resolve only active and valid entries.
2. Respect currency and VAT mode.
3. Choose the highest applicable quantity break not exceeding requested quantity.
4. Return a single effective unit price.

## Inventory Resolution Rules

Minimum v1 resolution inputs:

- market
- variant
- required quantity

Rules:

1. Consider only inventory locations assigned to the market.
2. Sum available stock across eligible locations unless future business rules require priority/allocation logic.
3. If insufficient available stock and no backorder rule exists, checkout must block.
4. Optional reservation is created during checkout start or payment initiation.

## Culture and Translation Rules

1. A market has one default culture.
2. Translatable entity reads should request a culture explicitly.
3. If translation is missing, fallback order is:
   - requested culture
   - market default culture
   - platform default culture
4. Custom fields marked localized follow the same fallback policy.

## Customer and Company Rules

1. A customer may shop as an individual or in company context.
2. Company context must come from an active membership.
3. Company permissions affect whether the customer can place orders, approve them, or manage users.
4. Future approval workflows should build on `CompanyMembership`, not replace it.

## Cart to Order Flow

Recommended v1 flow:

1. Create or load cart.
2. Set market, currency, and culture.
3. Add/update cart lines.
4. Resolve price and availability on change.
5. Attach customer and optional company context.
6. Set addresses, payment method, and shipping method.
7. Validate cart before checkout.
8. Optionally create inventory reservations.
9. Initiate payment with provider.
10. On successful authorization or accepted offline payment, create order from cart snapshot.
11. Consume or transform inventory reservations.
12. Mark cart as converted.

Rules:

1. Order creation must be idempotent.
2. Repeated payment callbacks must not duplicate orders.
3. The order snapshot must persist line names, SKU, prices, tax, and addresses.

## Permission Model

Minimum v1 permission roles:

- `PlatformAdmin`
- `CatalogManager`
- `PricingManager`
- `InventoryManager`
- `CustomerService`
- `IntegrationClient`
- `CompanyAdmin`
- `CompanyBuyer`
- `CompanyApprover`
- `Customer`

Examples:

- `PlatformAdmin`: full access
- `CatalogManager`: manage products, categories, translations, media
- `PricingManager`: manage price lists and entries
- `InventoryManager`: manage balances and transactions
- `CustomerService`: view/update customers, companies, carts, orders
- `IntegrationClient`: API-only scoped access
- `CompanyAdmin`: manage company users and company data
- `CompanyBuyer`: place orders for company
- `CompanyApprover`: approve carts/orders in future approval flows
- `Customer`: manage own profile, carts, and orders

## v1 API Surface Expectations

Admin/integration API should cover:

- customers
- companies
- memberships
- products
- variants
- categories
- translations
- markets
- price lists
- inventory locations and balances
- carts
- orders
- webhooks
- import/export jobs

Storefront API should cover:

- market context resolution
- category/product browsing
- product detail by slug or id
- price and availability reads
- cart create/load/update
- checkout initiation
- payment status polling/callback support

Backoffice should cover:

- product and variant administration
- media/image administration
- product status definition administration
- AI-assisted content generation and review
- pricing and inventory administration
- customer/company/order administration
- integration monitoring

## Explicitly Deferred from v1

These should not shape the first schema too much:

- promotion engine
- coupon engine
- advanced discount stacking
- returns and RMA
- subscriptions
- bundle/configurator logic
- advanced warehouse allocation
- multi-step company approval workflows
- CMS/page composition

## Recommended Next Artifact

After this document, the next useful artifact is:

1. an ERD/table design
2. API resource definitions
3. a generated `.NET 10` solution skeleton based on these modules
