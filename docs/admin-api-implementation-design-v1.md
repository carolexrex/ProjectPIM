# Admin API Implementation Design v1

## Purpose

This document defines how the admin API should be implemented in the `.NET 10` solution.

It covers:

- controller boundaries
- application service boundaries
- command/query patterns
- module ownership
- cross-cutting concerns
- implementation rules

This is the implementation-oriented companion to [admin-api-contract-v1.md](c:\Users\SpotonAlexV\source\repos\Projekt PIM\docs\admin-api-contract-v1.md).

## Goals

The implementation should be:

- modular
- easy to navigate
- testable
- explicit about business rules
- suitable for future extraction if needed

The implementation should not:

- start with microservices
- hide everything behind excessive generic repositories
- collapse all logic into controllers

## Recommended Solution Structure

```text
/src
  /Platform.Api
  /Platform.Application
  /Platform.Domain
  /Platform.Infrastructure
  /Platform.Contracts
```

For admin API specifically:

- `Platform.Api` hosts controllers and HTTP concerns
- `Platform.Application` contains use cases
- `Platform.Domain` contains domain models and business rules
- `Platform.Infrastructure` contains persistence, providers, and integrations
- `Platform.Contracts` contains DTOs if you want a separate contract assembly

## Module Boundaries

The admin API should follow module boundaries, not one giant controllers folder.

Recommended modules:

- `Catalog`
- `Markets`
- `Pricing`
- `Inventory`
- `Customers`
- `Companies`
- `Cart`
- `Orders`
- `CustomFields`
- `Ai`

## Controller Design

Controllers should be:

- thin
- module-aligned
- responsible for HTTP mapping only

Controllers should not:

- contain business logic
- talk directly to EF Core DbContext for non-trivial operations
- implement pricing or stock rules inline

## Recommended Controllers

## Catalog

- `ProductsController`
- `ProductTranslationsController`
- `VariantsController`
- `CategoriesController`
- `CategoryTranslationsController`
- `BrandsController`
- `ProductMediaController`
- `ProductStatusDefinitionsController`

## Markets

- `MarketsController`
- `MarketProductsController`
- `MarketPriceListsController`
- `MarketInventoryLocationsController`

## Pricing

- `PriceListsController`
- `PriceListEntriesController`

## Inventory

- `InventoryLocationsController`
- `InventoryBalancesController`
- `InventoryTransactionsController`

## Customers and Companies

- `CustomersController`
- `CustomerAddressesController`
- `CompaniesController`
- `CompanyAddressesController`
- `CompanyMembershipsController`

## Cart and Orders

- `CartsController`
- `OrdersController`
- `OrderStatusController`
- `PaymentTransactionsController`

## Custom Fields

- `CustomFieldsController`

## AI

- `AiPromptTemplatesController`
- `AiJobsController`
- `AiSuggestionsController`

## Routing Style

Prefer explicit routes that mirror the contract.

Examples:

- `/api/admin/products`
- `/api/admin/products/{id}/translations/{cultureCode}`
- `/api/admin/markets/{marketId}/price-lists/{priceListId}`
- `/api/admin/orders/{id}/status`

Do not over-nest routes unless the parent-child relationship is important to the operation.

## Application Layer Design

Use application services organized by module and operation type.

Two good options:

1. feature folders with commands/queries
2. module services with internal command/query handlers

Recommended v1 approach:

- feature folders inside each module
- MediatR-style or lightweight handler pattern is fine

Example structure:

```text
/Platform.Application
  /Catalog
    /Products
      /Queries
        GetProductByIdQuery.cs
        ListProductsQuery.cs
      /Commands
        CreateProductCommand.cs
        UpdateProductCommand.cs
        ArchiveProductCommand.cs
        AssignProductStatusCommand.cs
    /Variants
    /Categories
    /Brands
    /Media
  /Markets
  /Pricing
  /Inventory
  /Customers
  /Companies
  /Cart
  /Orders
  /CustomFields
  /Ai
```

## Command and Query Rules

## Commands

Commands should:

- change state
- validate intent
- enforce business rules
- publish domain/integration events when relevant

Commands should return:

- created IDs
- updated summaries
- or a simple success result

Examples:

- `CreateProductCommand`
- `UpdateVariantCommand`
- `AssignProductStatusCommand`
- `UpsertPriceListEntryCommand`
- `AdjustInventoryCommand`
- `CreateCompanyMembershipCommand`
- `ChangeOrderStatusCommand`
- `AcceptAiSuggestionCommand`

## Queries

Queries should:

- return admin-facing read models
- shape data for UI or integration needs
- not mutate state

Examples:

- `ListProductsQuery`
- `GetProductDetailsQuery`
- `ListOrdersQuery`
- `GetVariantInventorySnapshotQuery`
- `ListAiSuggestionsQuery`

## Read Models vs Domain Models

Do not serialize domain entities directly from controllers.

Use:

- command DTOs for write requests
- query/read DTOs for responses

Reason:

- admin screens need joined, shaped views
- domain entities should stay focused on business behavior

## Module Ownership

## Catalog Module Owns

- products
- variants
- product translations
- categories
- brands
- media attachments
- product status definitions

Catalog module should not own:

- effective price resolution
- inventory calculations
- market assignment decisions beyond validation hooks

## Markets Module Owns

- markets
- market currencies
- market cultures
- product availability in markets
- market price list assignments
- market inventory location assignments

## Pricing Module Owns

- price lists
- price entries
- validation of price list currencies and quantity breaks
- price resolution services for admin diagnostics

## Inventory Module Owns

- locations
- balances
- reservations
- transactions
- availability diagnostics

## Customers Module Owns

- customers
- customer addresses

## Companies Module Owns

- companies
- company addresses
- company memberships
- company permission flags

## Orders Module Owns

- orders
- order status history
- payment transactions

## Cart Module Owns

- carts
- cart lines
- cart repricing and expiration actions

## CustomFields Module Owns

- custom field definitions
- custom field value orchestration

## Ai Module Owns

- prompt templates
- AI generation jobs
- AI suggestions
- suggestion review workflow

## Orchestration Rules

Some operations cross module boundaries.

Those should be coordinated in the application layer, not by letting one module reach deep into another module's persistence.

Examples:

### Create Product

Primary owner:

- Catalog

May validate with:

- CustomFields
- Markets if default market assignments are included

### Assign Product To Market

Primary owner:

- Markets

May validate with:

- Catalog

### Reprice Cart

Primary owner:

- Cart

Uses services from:

- Pricing
- Inventory
- Markets

### Change Order Status

Primary owner:

- Orders

May emit:

- integration events
- audit records

### Accept AI Suggestion

Primary owner:

- Ai

May write into:

- Catalog translated fields
- Custom field values

This should be implemented through a target field writer abstraction, not direct controller branching.

## Recommended Interfaces

Examples of useful interfaces:

```csharp
public interface IProductRepository { }
public interface IVariantRepository { }
public interface IPriceListRepository { }
public interface IInventoryBalanceRepository { }
public interface IOrderRepository { }
public interface IUnitOfWork { }
public interface IClock { }
public interface ICurrentUserAccessor { }
public interface IEventPublisher { }
public interface IAuditWriter { }
public interface IAiSuggestionTargetWriter { }
```

Keep repositories module-oriented, not generic.

Avoid:

- `IGenericRepository<T>`

That usually weakens business clarity.

## EF Core Design

Recommended:

- one main DbContext in v1
- entity configuration classes per entity
- module-based folders for configurations

Example:

```text
/Platform.Infrastructure/Persistence
  /PlatformDbContext.cs
  /Configurations
    /Catalog
    /Markets
    /Pricing
    /Inventory
    /Customers
    /Companies
    /Cart
    /Orders
    /CustomFields
    /Ai
```

Queries can use:

- EF Core projections
- Dapper for heavy admin list screens if needed later

## Validation

Use layered validation:

1. HTTP/request validation
2. application validation
3. domain rule validation
4. database constraints

Suggested tools:

- `FluentValidation` for request/command validation

Examples:

- duplicate SKU checks
- invalid market currency assignment
- invalid product status assignment
- invalid quantity break values
- illegal order state transition

## Transactions

Use a database transaction for operations such as:

- creating orders from carts
- inventory adjustment plus transaction record
- accepting AI suggestions into live content
- multi-record price updates

Prefer application-service transaction boundaries over transaction logic inside controllers.

## Mapping

Use explicit mapping between:

- HTTP request DTOs
- commands
- domain objects
- read models

AutoMapper is optional. Manual mapping is acceptable and often clearer early on.

## Authorization Design

Use policy-based authorization at the controller or endpoint level.

Examples:

- `CatalogWrite`
- `PricingWrite`
- `InventoryWrite`
- `CustomerServiceWrite`
- `OrderStatusWrite`
- `AiContentReview`

Fine-grained checks can still happen in handlers for company/market scoped rules.

## Auditing

Important commands should write audit records.

Examples:

- product created/updated
- status changed
- price changed
- inventory adjusted
- membership changed
- order status changed
- AI suggestion accepted/rejected

Audit writing should be application-layer infrastructure, not duplicated in every controller.

## Events

Use internal domain events and external integration events.

Examples:

- `ProductStatusChanged`
- `PriceListEntryUpserted`
- `InventoryAdjusted`
- `OrderStatusChanged`
- `CompanyMembershipChanged`
- `AiSuggestionAccepted`

Use the outbox pattern for external delivery.

## Recommended Implementation Pattern Per Endpoint

Example: `POST /api/admin/products`

1. controller receives request DTO
2. request DTO is validated
3. controller maps to `CreateProductCommand`
4. handler loads needed references
5. handler enforces business rules
6. handler saves entity through repositories/unit of work
7. handler returns result DTO
8. controller returns `201 Created`

Example: `GET /api/admin/orders`

1. controller receives filters
2. controller maps to `ListOrdersQuery`
3. query handler projects optimized admin read model
4. controller returns paged response

## Anti-Patterns To Avoid

1. fat controllers
2. one service class with hundreds of methods
3. generic repositories for everything
4. direct DbContext mutations from controllers
5. leaking EF entities to API responses
6. embedding workflow rules in the UI only
7. skipping row-version concurrency on mutable admin writes

## Suggested First Pass Implementation Order

1. products
2. product translations
3. variants
4. categories
5. brands
6. product status definitions
7. markets
8. price lists and entries
9. inventory locations and balances
10. customers
11. companies and memberships
12. orders
13. carts
14. custom fields
15. AI admin endpoints

This order gives you a working backoffice core quickly.

## Recommended Next Step

After this document, the next useful artifact is:

1. concrete `.NET 10` project skeleton
2. EF Core entity/configuration generation
3. controller/request/response class skeletons
